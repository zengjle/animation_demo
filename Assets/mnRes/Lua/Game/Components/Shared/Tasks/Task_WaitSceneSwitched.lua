--[[
任务-等待场景切换成功
]]

Task_WaitSceneSwitched = Class(Task)

local waitSceneSwitched = 'waitSceneSwitched'

function Task_WaitSceneSwitched:ctor(factor, mapId, ...)
	self.name = 'wait scene switched'

	TaskManager.RegisterSignal(waitSceneSwitched)
end

function Task_WaitSceneSwitched:OnStart()
	EventManager:AddListener(EventTypes.System_SceneLoadFinished, self.onSceneLoadFinished, self)
	self:OnFinished()
end

function Task_WaitSceneSwitched:Cleanup()
	EventManager:RemoveListener(EventTypes.System_SceneLoadFinished, self.onSceneLoadFinished, self)
end

function Task_WaitSceneSwitched:onSceneLoadFinished()
	TaskManager.Signal(waitSceneSwitched)
end