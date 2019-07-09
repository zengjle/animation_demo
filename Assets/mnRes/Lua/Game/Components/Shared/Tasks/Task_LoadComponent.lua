--[[
任务-载入模块
]]

Task_LoadComponent = Class(Task)

function Task_LoadComponent:ctor(factor, component, handler, ...)
	self.name = 'load Component'
	self.component = component
	self.factor = factor
	self.handler = handler
	self.args = {...}
end

function Task_LoadComponent:OnStart()
	self.component:Active(false, function()
		print('------ task load component')
		if self.handler then
			self.handler()
		end

		self:OnFinished()
	end, true, table.unpack(self.args))
end