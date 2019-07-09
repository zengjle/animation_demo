--[[
任务-激活场景
]]

Task_ActiveScene = Class(Task)

function Task_ActiveScene:ctor(factor)
	self.name = 'active scene'
	self.factor = factor
end

function Task_ActiveScene:OnStart()
	self:OnFinished()
	
	local ao = Game.Shared.tasks.loadSceneAO 
	if not ao then
		return
	end

	CSCoroutine.Start(function ()
		coroutine.yield(Delay.wait500MilliSeconds)

		ao.allowSceneActivation = true

		local context = Game.Shared.tasks.nextContext
		if context then
			ViewManager.SwitchContext(context.name)
		end

		Game.Shared.tasks.loadSceneAO = nil
		ViewManager.SetNextContext(nil)
	end)
end
