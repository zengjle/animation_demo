--[[
Context是一种游戏的上下文情景，这里特指管理View的上下文情景，比如登录，大厅，游戏内分别就是独立的Context，可以通过Context，方便恢复View的上下文情景
]]

Context = Class()

Context.name = '__undefined_context__'
Context.path = '__undefined_context_path__'

Context.stacks = nil

function Context:ctor(name, path, root)
	self.name = name
	self.path = path
	self.stacks = {}
	self.root = root

	for i = 1, #Config.ViewLayer.configs do
		self.stacks[i] = Stack.New(name .. '_' .. i)
	end

	print('[Context] context ' .. name .. ' has created')
end

function Context:OnEnable()
	print('[Context] ' .. self.name .. ' enabled')
end

function Context:OnDisable()
	print('[Context] ' .. self.name .. ' disabled')
	
	if self.root then
		self.root:Destroy(true)
	else
		for i = 1, #self.stacks do
			local stack = self.stacks[i]
			for j = 1, #stack do
				local view = stack[j]
				if view.controller then
					view.controller:Destroy(true)
				else
					warn('no controller, confirm pls, context ' .. self.name .. ', view name ' .. view.name)
				end
			end		
		end
	end

	self.stacks = {}

	for i = 1, #Config.ViewLayer.configs do
		self.stacks[i] = Stack.New(self.name .. '_' .. i)
	end
end

function Context:OnDestroy()
	print('[Context] ' .. self.name .. ' destroy')
end

function Context:PushView(view, layer, anim)
	local stack = self.stacks[layer]

	stack:Traverse(function(v)
		v.go:SetActive(false) 
	end)

	stack:Push(view)
	view.go:SetActive(true)

	print('[Context] ' .. self.name .. ' push view ' .. view.name)
end

function Context:PopView(view, layer, anim, ignoreTop)
	local stack = self.stacks[layer]

	stack:Remove(view)
	view.go:SetActive(false)

	if not ignoreTop then
		stack:Traverse(function(v, i)
			v.go:SetActive(i == 1)
		end)
	end

	print('[Context] ' .. self.name .. ' pop view ' .. view.name)
end

-- 获取指定层以下顶级view
function Context:GetTopView(topLayer)
	for i = topLayer, 1, -1 do
		if self.stacks[i]:Top() then
			return self.stacks[i]:Top()
		end
	end

	return nil
end