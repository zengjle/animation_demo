--[[
任务-设置下一个context
]]

Task_NextContext = Class(Task)

function Task_NextContext:ctor(factor, context)
	self.name = 'next context'
	self.factor = factor

	ViewManager.SetNextContext(context)
end

function Task_NextContext:OnStart()
	self:OnFinished()
end