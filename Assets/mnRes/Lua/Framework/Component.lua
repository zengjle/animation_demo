--[[
Component机制
Component是一种树形结构组织的节点，从物理上可以理解对应一个挂载LuaTarget的Prefab
Element是指Prefab中的子节点，不需要被独立载入Prefab功能节点，这个节点需要挂载LuaTarget

Version 0.2.1
]]

local compNames = {}
local viewConstants = Config.ViewConstants
local tagComponents = {}

--[[
elementType = viewConstants.top，则为有独立的prefab，是顶级节点，挂载的层由layer定义
elementType = viewConstants.node，有独立的perfab,但是被载入挂接到相对节点，相对节点的路径由layer控制，
elementType = viewConstants.element, 则代表是一个在Prefab的子节点，不需要独立载入prefab
baseDir 代表游戏的启动目录入口，只需要根节点设置即可，其他子节点不需要设定
]]
function Component(name, elementType, linkRes, layer, tag, baseDir)
	local tbl = {}
	print(name)
	-- 组件名，全局唯一，方便调试跟踪
	if elementType ~= viewConstants.element  then
		if compNames[name] then
			error('[Component] component ' .. name ..  ' has existed!')
			return nil
		end
		
		compNames[name] = true
	end
	
	tbl.__name__ = name and name or "__undefined_component__"
	tbl.elementType = elementType and elementType or viewConstants.top
	tbl.linkRes = linkRes and linkRes or ''
	tbl.baseDir = baseDir and baseDir or ''
	tbl.components = {}
	tbl.elementLookups = {}
	tbl.plugins = {}
	tbl.actived = false
	tbl.everDestroyed = false
	tbl.everActived = false
	if tbl.elementType == viewConstants.top then
		tbl.layer = layer and Name2ViewLayer(layer) or 1
	else
		tbl.layer = layer and layer or ''
	end
	

	tbl.tag = tag and tag or ''

	if #tbl.tag > 0 then
		if tagComponents[tbl.tag] then
			error('component tag has existed, named ' .. tbl.tag)
		end

		tagComponents[tbl.tag] = tbl
	end

	tbl.elements = {
		baseDir = tbl.baseDir,
		name = tbl.__name__,
		ptr = tbl
	}

	tbl.viewValid = function (self)
		return self.__view__ ~= nil
	end

	-- 注册Element到Component
	tbl.Element = function(self, name, target, handler)
		if not target then
			error('element ' .. name .. ' mapping to invalid target')
			return
		end

		if self.elementLookups[name] then
			warn('[Component] element ' .. name .. ' has registered before, confirm pls')
		end

		self.elementLookups[name] = {name = target, handler = handler}
	end

	-- 激活模块, sync代表是否要采用同步或者异步的方式载入资源
	tbl.Active = function(self, sync, handler, silent, ...)
		if self.actived then
			warn('[Component] component ' .. self.__name__ .. ' has actived, do not active again, confirm pls')
			return
		end

		self.actived = true
		self.everDestroyed = false

		local activeViewCreated = not self.everActived

		if not self.everActived then
			self.everActived = true

			local onAwake = rawget(self, 'OnAwake')
			if onAwake and type(onAwake) == 'function' then
				self:OnAwake(...)
			end
		end

		local pluginCall = function (initor)
			for i = 1, #self.plugins do
				local p = self.plugins[i]
				local initKey = '__' .. initor .. '__'
				if not p[initKey] and p[initor] and type(p[initor]) == 'function' then
					p[initKey] = true
					p[initor](p)
				end
			end
		end

		local afterViewLoaded = function()
			if activeViewCreated then
				local onViewCreated = rawget(self, 'OnViewCreated')
				if onViewCreated and type(onViewCreated) then
					self:OnViewCreated()
				end
			end

			local onViewLoaded = rawget(self, 'OnViewLoaded')
			if onViewLoaded and type(onViewLoaded) then
				self:OnViewLoaded()
			end

			pluginCall('delayInit')
		end

		local broadcast = function ()
			if silent or not self.tag or #self.tag == 0 or self.tag == viewConstants.tagLoading then
				return
			end

			EventManager:Broadcast(EventTypes.System_ViewActive, self.tag)
		end

		-- 先触发OnEnable
		local onEnable = rawget(self, 'OnEnable')
		if onEnable and type(onEnable) == 'function' then
			self:OnEnable(...)
		end

		pluginCall('init')

		-- 如果是模块，则载入并进入ViewManager
		if self.elementType == viewConstants.top then
			if self:viewValid() then
				ViewManager.PushView(self.__view__.__internal__, self.layer)

				-- 激活父节点后，开始自动激活Elements
				for k, v in pairs(self.elementLookups) do
					local element = self.elements[k]
					if element then
						element:Active(true, v and v.handler or nil)
					end
				end

				if handler then
					handler()
				end

				afterViewLoaded()
				broadcast()
			else
				if #self.linkRes > 0 then
					ViewManager.CreateView(self.linkRes, sync, self.tag, self, function(view)
						-- 创建内部view引用结构，支持.view写法
						self.__view__ = {}
						self.__view__.__internal__ = view
						if view.__lua_target__ then
							self.__view__.__lua_table__ = view.__lua_target__.Table
						end

						ViewManager.PushView(view, self.layer)

						-- 激活父节点后，开始自动激活Elements
						for k, v in pairs(self.elementLookups) do
							local element = self.elements[k]
							if element then
								element:Active(true, v and v.handler or nil)
								v.element = element
							end
						end

						if handler then
							handler()
						end

						afterViewLoaded()
						broadcast()
					end)
				else
					if handler then
						handler()
					end

					afterViewLoaded()
					broadcast()
				end
			end
		elseif self.elementType == viewConstants.node then
			-- 子组件在一定要在父节点初始化后，才可以使用
			if not self.up or not self.up.view then
				warn('[Component] parent of sub component {' .. self.__name__ .. '} have not actived')
				return
			end

			-- view有效证明已经载入过了
			if self:viewValid() then
				-- 激活父节点后，开始自动激活Elements
				for k, v in pairs(self.elementLookups) do
					local element = self.elements[k]
					if element then
						element:Active(true, v and v.handler or nil)
					end
				end

				if handler then
					handler()
				end

				if not self.__view__.__internal__.go.activeSelf then
					self.__view__.__internal__.go:SetActive(true)
				end

				afterViewLoaded()
				broadcast()
			else
				ViewManager.CreateView(self.linkRes, sync, self.tag, self, function (view)
					-- 创建内部view引用结构，支持.view写法
					self.__view__ = {}
					self.__view__.__internal__ = view
					if view.__lua_target__ then
						self.__view__.__lua_table__ = view.__lua_target__.Table
					end

					local p = self.up.__view__.__internal__.go.transform:Find(self.layer) or self.up.__view__.__internal__.go.transform
						print(type(self.layer)..",,解锁,,,,layer:"..self.layer)
					if not p then
						warn('[Component] component ' .. self.__name__ .. ' fail to get path ' .. self.layer .. ' to mount to parent')
					end

					view.go.transform:SetParent(p, false)

					-- 激活父节点后，开始自动激活Elements
					for k, v in pairs(self.elementLookups) do
						local element = self.elements[k]
						if element then
							element:Active(true, v and v.handler or nil)
							v.element = element
						end
					end

					if handler then
						handler()
					end

					if not self.__view__.__internal__.go.activeSelf then
						self.__view__.__internal__.go:SetActive(true)
					end

					afterViewLoaded()
					broadcast()
				end)
			end
		else
			-- 元素在一定要在父节点初始化后，才可以使用
			if not self.up or not self.up.view then
				warn('[Component] parent of element {' .. self.__name__ '} have not actived')
				return
			end
			if not self:viewValid() and self.linkRes then
				-- 创建内部view引用结构，支持.view写法
				local view = ViewManager.GenElementView(self.up.view.args[self.linkRes], self.linkRes, self)

				self.__view__ = {}
				self.__view__.__internal__ = view

				if view.__lua_target__ then
					self.__view__.__lua_table__ = view.__lua_target__.Table
				end
			end

			if handler then
				handler()
			end

			if not self.__view__.__internal__.go.activeSelf then
				self.__view__.__internal__.go:SetActive(true)
			end

			afterViewLoaded()
		end
	end

	-- 隐藏模块
	tbl.Deactive = function (self, ignoreTop, ...)
		if not self.actived then
			return
		end

		self.actived = false

		for k, v in pairs(self.components) do
			v:Deactive(...)
		end

		self.cells:deactive()

		-- 把子元素触发Deactive
		for k, v in pairs(self.elementLookups) do
			local element = self.elements[k]
			if element then
				element:Deactive(...)
			end
		end

		if self:viewValid() then
			if self.elementType == viewConstants.top then
				ViewManager.PopView(self.__view__.__internal__, self.layer, ignoreTop)
			else
				self.__view__.__internal__.go:SetActive(false)
			end
		end

		local onDisable = rawget(self, 'OnDisable')
		if onDisable and type(onDisable) == 'function' then
			self:OnDisable(...)
		end
	end

	-- 删除模块
	tbl.Destroy = function (self, isolate, ...)
		if self.everDestroyed then
			warn('[Component] has destroyed before, do not destroy again, confirm pls')
			return
		end

		for k, v in pairs(self.components) do
			v:Destroy(isolate, ...)
		end

		self.cells:destroy()
		
		for k, v in pairs(self.elementLookups) do
			local element = self.elements[k]
			if element then
				element:Destroy(isolate, ...)
			end
		end

		local hasActived = self.actived

		self.actived = false
		self.everDestroyed = true
		self.everActived = false

		if self:viewValid() then
			if self.elementType == viewConstants.top and not isolate then
				ViewManager.PopView(self.__view__.__internal__, self.layer)
			end

			GameObject.Destroy(self.__view__.__internal__.go)
			self.__view__.__internal__ = nil
			self.__view__ = nil
		end

		local pluginCall = function (initor)
			for i = 1, #self.plugins do
				local p = self.plugins[i]
				p['__init__'] = false
				p['__delayInit__'] = false
				if p[initor] and type(p[initor]) == 'function' then
					p[initor](p)
				end
			end
		end

		pluginCall('destroy')

		if hasActived then
			local onDisable = rawget(self, 'OnDisable')
			if onDisable and type(onDisable) == 'function' then
				self:OnDisable(...)
			end
		end

		local onDestroy = rawget(self, 'OnDestroy')
		if onDestroy and type(onDestroy) == 'function' then
			self:OnDestroy(...)
		end
	end

	-- 添加插件，这个接口只给插件系统内部使用
	tbl.addPlugin = function (self, plugin)
		self.plugins[#self.plugins + 1] = plugin
	end

	-- 组件自动载入功能
	setmetatable(tbl, {
		__index = function(t, k)
			-- view的获取定向为真正的luatarget
			print('载入.............'..k)
			if k == 'view' then
				local v = rawget(t, '__view__')
				if not v then
					return nil
				end

				return v.__lua_table__
			elseif k == 'model' then
				local m = rawget(t, '__model__')
				if not m then
					local dirs = Split(t.baseDir, "([^'.']+)")
					local path = t.baseDir .. '.' .. dirs[#dirs] .. 'Model'

					m = require(path).New()
					if m then
						rawset(t, '__model__', m)
					end
				end

				return m
			end

			local path = t.baseDir .. '.Components.' .. k .. '.' .. k

			-- 对应的脚本存在，才是component
			if not LuaManager:CheckScriptFileExisted(path) then
				return nil
			end

			local comp = require(path)

			if not comp then
				error('[Component] failed to get component '  .. k .. ' in ' .. t.__name__)
				return nil
			end

			comp.baseDir = t.baseDir .. '.Components.' .. k
			comp.elements.baseDir = comp.baseDir
			comp.cells.baseDir = comp.baseDir
			comp.up = t

			t[k] = comp
			t.components[k] = comp

			return comp
		end
	})

	-- Element自动载入功能
	setmetatable(tbl.elements, {
		__index = function(t, k)
			local path = t.baseDir .. '.Elements.' .. k .. '.' .. k
			local element = require(path)

			if not element then
				error('[Component] fail to get element ' .. k .. ' in ' .. t.name)
				return nil
			end

			-- 没有使用Element函数绑定过，无法找到对应的节点进行对应
			local lookup = t.ptr.elementLookups[k]
			if lookup then
				element.linkRes = lookup.name
			else
				warn('[Component] element {' .. k .. '} did not have gameObject to bind')
			end

			element.up = t.ptr

			t[k] = element
			return element
		end
	})

	-- Cell
	tbl.cells = {
		baseDir = tbl.baseDir,
		ptr = tbl,

		deactive = function (self)
		end,

		destroy = function (self)
			for k, v in pairs(self) do
				if type(v) == 'table' and v.destroy then
					v:destroy()
				end
			end
		end,
	}

	-- Cell自动载入功能
	setmetatable(tbl.cells, {
		__index = function (t, k)
			local path = t.baseDir .. '.Cells.' .. k .. '.' .. k
			local cell = require(path).New(t.ptr)

			if not cell then
				warn('[Component] cell ' .. path .. ' not found, confirm pls')
				return nil
			end

			if cell.__signature__ ~= SignatureForCell() then
				error('[Component] cell ' .. path .. ' is not cell, confirm pls')
				return nil
			end

			local cells = {}

			local activation = function(cell)
				cell:active()
			end

			local deactivation = function (cell)
				cell:deactive()
			end

			local destroy = function(self, cell)
				self.__pool__:free(cell)
			end

			-- cell内存池
			cells.__pool__ = {
				new = function(self, up, pool, ...)
					if #self > 0 then
						local c = self[#self]
						self[#self] = nil
						if c.ctor then
							c.ctor(c, up, pool, ...)
						end

						return c
					else
						if not self.__prototype__ then
							self.__prototype__ = require(t.baseDir .. '.Cells.' .. k .. '.' .. k)
						end

						return self.__prototype__ and self.__prototype__.New(up, pool, ...) or nil
					end
				end,

				free = function(self, c)
					if not c then
						return
					end

					c:deactive()
					c:destroy()

					self[#self + 1] = c
				end,

				destroy = function (self)
					while #self > 0 do
						table.remove(self, 1)
					end
				end
			}

			-- cell.view内存池
			cells.__view_pool__ = {
				new = function (self, sync, controller, handler)
					if #self > 0 then
						local c = self[#self]
						self[#self] = nil

						ViewManager.ResetView(c, controller)
						if handler then
							handler(c)
						end
					else
						-- prototype从未被载入过，先载入再进行实例化
						if not self.__prototype__ then
							local cp = require(t.baseDir .. '.Cells.' .. k .. '.' .. k)
							ViewManager.LoadViewPrototype(cp.linkRes, sync, function (prefab)
								self.__prototype__ = prefab
								local v = ViewManager.CreateViewByPrefab(prefab, false, controller)

								if handler then
									handler(v)
								end
							end)
						else
							local v = ViewManager.CreateViewByPrefab(self.__prototype__, false, controller)
							if handler then
								handler(v)
							end
						end
					end
				end,

				free = function (self, v)
					if not v then
						return
					end

					ViewManager.ResetView(v, nil)

					self[#self + 1] = v
				end,

				destroy = function (self)
					while #self > 0 do
						table.remove(self, 1)
					end
				end
			}

			cells.allocView = function(self, sync, controller, handler)
				self.__view_pool__:new(sync, controller, handler)
			end

			cells.freeView = function(self, v)
				self.__view_pool__:free(v)
			end

			-- 一次性分配
			cells.SetCapacity = function (self, n, beforeActived, afterActived)
				local path = t.baseDir .. '.Cells.' .. k .. '.' .. k
				local num = (n and n > 0) and n or 0
				local cell = require(path)

				-- 已经存在就触发自定义handler就行了
				for i = 1, math.min(n, #self) do
					local item = self[i]

					deactivation(item)

					if beforeActived then
						beforeActived(item, i)
					end

					activation(item)

					if afterActived then
						afterActived(item, i)
					end
				end

				-- 不足添加
				for i = #self + 1, n do
					local item = self.__pool__:new(t.ptr, self)
					self[#self + 1] = item
					if beforeActived then
						beforeActived(item, i)
					end

					activation(item)

					if afterActived then
						afterActived(item, i)
					end
				end

				-- 太多了，就删除咯
				for i = n + 1, #self do
					destroy(self, self[i])
					self[i] = nil
				end

				return self
			end

			-- 添加item
			cells.Add = function(self, n, beforeActived, afterActived)
				local path = t.baseDir .. '.Cells.' .. k .. '.' .. k
				local num = (n and n > 0) and n or 0

				for i = 1, num do
					local item = self.__pool__:new(t.ptr, self)
					if beforeActived then
						beforeActived(item, i)
					end

					self[#self + 1] = item
					activation(item)

					if afterActived then
						afterActived(item, i)
					end
				end
			end


			-- 释放指定item O(n)
			cells.Free = function(self, item)
				if not item then
					return
				end

				if item.__signature__ ~= SignatureForCell() then
					error('[Component] freed item is not cell, confirm pls')
					return nil
				end

				local itemIdx = 0
				-- 删除指定，后面移位
				for i = 1, #self do
					if itemIdx ~= 0 then
						self[i-1] = self[i]
					elseif item == self[i] then
						itemIdx = i
						destroy(self, item)
						self[i] = nil
					end
				end

				if #self > 0 then
					table.remove(self, #self)
				end
			end

			cells.Clear = function (self)
				for i = 1, #self do
					if self[i].__signature__ == SignatureForCell() then
						destroy(self, self[i])
					end
				end

				while #self > 0 do
					table.remove(self, 1)
				end
			end

			-- 删除所有
			cells.destroy = function(self)
				for i = 1, #self do
					if self[i].__signature__ == SignatureForCell() then
						destroy(self, self[i])
					end
				end

				while #self > 0 do
					table.remove(self, 1)
				end

				self.__view_pool__:destroy()
				self.__pool__:destroy()
			end

			rawset(t, k, cells)
			return cells
		end
	})

	return tbl
end

-- 根据tag获取component
function GetComponent(tag)
	return tagComponents[tag]
end