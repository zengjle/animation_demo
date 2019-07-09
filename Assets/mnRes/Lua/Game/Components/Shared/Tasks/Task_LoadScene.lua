--[[
任务-载入场景
]]

Task_LoadScene = Class(Task)

function Task_LoadScene:ctor(factor, context, allowSceneActivation)
	self.name = 'load scene'
	self.sceneName = context.name
	self.path = context.path
	self.factor = factor
	self.allowSceneActivation = allowSceneActivation

	Game.Shared.tasks.nextContext = context
end

function Task_LoadScene:OnStart()
	CoroutineManager:StartCoroutine(
		ResourceManager:LoadSceneAsync(self.path, self.sceneName, 
			function(ao)
				print('[Task_LoadScene] start load scene')
				Game.Shared.tasks.loadSceneAO = ao
				ao.allowSceneActivation = self.allowSceneActivation
			end,
			
			function(progress)
				print('[Task_LoadScene] loading scene progess ' .. progress)
				self:OnProgress(progress)
			end,

			function(ao)
				print('[Task_LoadScene] finish load scene')
				self:OnProgress(1)
				self:OnFinished()
			end
		)
	)
end