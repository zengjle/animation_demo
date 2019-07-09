--[[
任务-激活Context
]]

Task_ActiveContext = Class(Task)

function Task_ActiveContext:ctor(factor)
	self.name = 'active context'
	self.factor = factor
end

function Task_ActiveContext:OnStart()
	self:OnFinished()

	CSCoroutine.Start(function ()
		coroutine.yield(Delay.wait500MilliSeconds)

		local context = Game.Shared.tasks.nextContext
		if context then
			ViewManager.SwitchContext(context.name)
		end

		ViewManager.SetNextContext(nil)
	end)
end
