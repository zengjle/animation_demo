--[[
任务-添加场景
]]

Task_AddScene = Class(Task)

function Task_AddScene:ctor(factor, path, sceneName)
    self.name = 'add scene'
    self.sceneName = sceneName
    self.path = path
    self.factor = factor
end

function Task_AddScene:OnStart()
    CoroutineManager:StartCoroutine(
        ResourceManager:LoadSceneAsync(self.path, self.sceneName, 
            function(ao)
                print('[Task_AddScene] start load scene')
            end,
            
            function(progress)
                print('[Task_AddScene] loading scene progess ' .. progress)
                self:OnProgress(progress)
            end,

            function(ao)
                print('[Task_AddScene] finish load scene')
                self:OnProgress(1)
                self:OnFinished()
            end,
            UnityEngine.SceneManagement.LoadSceneMode.Additive
        )
    )
end