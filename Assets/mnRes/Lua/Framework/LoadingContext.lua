--[[
用于载入相关的Context
]]

LoadingContext = Class()

LoadingContext.name = '__undefined_loading_context__'
LoadingContext.stack = nil

local config = Config.LoadingType

function LoadingContext:ctor(name)
	self.name = name
	self.path = path
	self.stacks = {}

	for i = 1, #Config.ViewLayer.configs do
		self.stacks[i] = Stack.New(name .. '_' .. i)
	end

	print('[LoadingContext] context ' .. self.name .. ' has created')
end

function LoadingContext:OnEnable()
	print('[LoadingContext] ' .. self.name .. ' enabled')
end

function LoadingContext:OnDisable()
	print('[LoadingContext] ' .. self.name .. ' disabled')
	
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

	self.stacks = {}

	for i = 1, #Config.ViewLayer.configs do
		self.stacks[i] = Stack.New(self.name .. '_' .. i)
	end
end

function LoadingContext:OnDestroy()
	print('[LoadingContext] ' .. self.name .. ' destroy')
end

function LoadingContext:PushView(view, layer, anim)
	local stack = self.stacks[layer]
	stack:Push(view)
	view.go:SetActive(true)
end

function LoadingContext:PopView(view, layer, anim)
	local stack = self.stacks[layer]

	stack:Remove(view)
	view.go:SetActive(false)
end