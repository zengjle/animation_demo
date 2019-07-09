--[[
任务-广播时间
]]

Task_BroadcastEvent = Class(Task)

function Task_BroadcastEvent:ctor(factor, event, ...)
	self.name = 'broadcast event'
	self.factor = factor
	self.event = event
	self.args = {...}
end

function Task_BroadcastEvent:OnStart()
	if self.event then
		EventManager:Broadcast(self.event, table.unpack(self.args))
	end

	self:OnFinished()
end