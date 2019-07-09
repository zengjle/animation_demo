--[[
 Block是一种数据集合，很像C语言中的struct，记住，没有操作，只有数据，重要的事情说3遍，1,2,3
]]

function Block(name, members, linkRes)
	local blk = {}

	BlockManager.register(name)

	blk.blockName = name
	blk.linkRes = linkRes
	blk.__signature__ = blockSignature

	blk.active = function (self)
		BlockManager.onBlockChanged(self, nil, nil, BlockState.added)
	end

	blk.New = function (self, members)
		local tbl = {isReady = false}

		for k, v in pairs(blk) do
			if v ~= nil and string.starts(k, '__') and string.ends(k, '__') and k ~= '__signature__' then
				tbl[k] = table.clone(v)
			end
		end

		setmetatable(tbl, {
			__index = function (t, k)
				if not string.starts(k, '__') and not string.ends(k, '__') then
					local newk = '__' .. k .. '__'
					local v = rawget(t, newk)
				
					if v then
						return v
					end
				end

				return blk[k]
			end,

			__newindex = function (t, k, v)
				if k == 'gameObject' then
					local lts = v:GetComponents(typeof(CS.XLua.LuaTarget))
					for i = 0, lts.Length - 1 do
						local disabled = false
						if not lts[i].Table then
							disabled = true
							lts[i].gameObject:SetActive(true)
						end

						if lts[i].Table then
							lts[i].Table.controller = t

							if blk.linkRes then
								local name = lts[i].luaFilename
								local dirs = Split(name, "([^'/']+)")
								if #dirs > 0 and dirs[#dirs]:stripExtension() == blk.linkRes then
									rawset(t, 'view', lts[i].Table)
								end
							end
						end

						if disabled then
							lts[i].gameObject:SetActive(false)
						end
					end
				elseif k == 'view' then
					error('block can not set view manual, confirm pls')
					return
				elseif k == 'ownerDestroyed' then
					if v then
						BlockManager.onTryEntityDestroy(t.entity, t)
					end
					return
				end

				local newk = '__' .. k .. '__'

				rawset(t, newk, v)
				if t.isReady then 
					BlockManager.onBlockChanged(t, k, v, BlockState.changed)
				end
			end})

		-- 自定义成员
		tbl:Set(members)

		-- 创建过程不触发事件
		tbl.isReady = true

		BlockManager.onBlockAlloced(tbl)

		return tbl
	end

	-- 批量赋值
	blk.Set = function (self, value)
		if not value then
			return
		end

		if type(value) ~= 'table' then
			error('block ' .. self.blockName .. ' call Set should be table as argument')
			return
		end
		
		for k, v in pairs(value) do
			if v ~= nil then
				self[k] = table.clone(v)
			end
		end
	end

	-- 类型判断
	blk.TypeOf = function (self, blk)
		return self.blockName == blk.blockName
	end

	blk.destroy = function (self)
		BlockManager.onBlockChanged(self, nil, nil, BlockState.deleted)
		BlockManager.onBlockFreed(self)
	end

	if members and type(members) == 'table' then
		for k, v in pairs(members) do
			blk['__' .. k .. '__'] = v
		end
	end

	return blk
end
