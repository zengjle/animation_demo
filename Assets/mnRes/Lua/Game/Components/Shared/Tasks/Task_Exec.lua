--[[
任务-执行
]]

Task_Exec = Class(Task)

function Task_Exec:ctor(factor, func)
	self.name = 'exec'
	self.factor = factor
	self.func = func
end

function Task_Exec:OnStart()
	if self.func then
		self.func()
	end

	self:OnFinished()
end
