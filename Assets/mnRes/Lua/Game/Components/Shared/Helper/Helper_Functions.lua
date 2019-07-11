--[[
  其他辅助功能
]]

Helper.Switch2Lobby = function (preLoaded, postLoaded)
    Game.Shared.CommonLoading:Active(true, function()
        TaskManager.Reset()

        TaskManager.AddTask(Task_Exec.New(1, function ()
            if preLoaded and type(preLoaded) == 'function' then
                preLoaded()
            end
        end))

        print("XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX")

        TaskManager.AddTask(Task_NextContext.New(1,ViewManager.GetContext('Lobby')))

        TaskManager.AddTask(Task_LoadComponent.New(1, Game.home))

        TaskManager.AddTask(Task_WaitSceneSwitched.New(1))

        TaskManager.AddTask(Task_LoadScene.New(1, ViewManager.GetContext('Lobby'), true))

        TaskManager.AddTask(Task_WaitAllSignals.New(1))

        TaskManager.AddTask(Task_ActiveContext.New(1))

        TaskManager.AddTask(Task_Exec.New(1, function ()
            if postLoaded and type(postLoaded) == 'function' then
                postLoaded()
            end
        end))

        TaskManager.Start()
    end)
end

-- 切换回Login
Helper.Switch2Login = function (preLoaded, postLoaded)
	Game.Shared.CommonLoading:Active(true, function()
        TaskManager.Reset()

        TaskManager.AddTask(Task_Exec.New(1, function ()
            if preLoaded and type(preLoaded) == 'function' then
                preLoaded()
            end
        end))

        TaskManager.AddTask(Task_NextContext.New(1,ViewManager.GetContext('Login')))

        TaskManager.AddTask(Task_LoadComponent.New(1, Game.Login))

		TaskManager.AddTask(Task_LoadScene.New(1, ViewManager.GetContext('Login'), false))

		TaskManager.AddTask(Task_ActiveScene.New(0.1))

        TaskManager.AddTask(Task_Exec.New(1, function ()
            if postLoaded and type(postLoaded) == 'function' then
                postLoaded()
            end
        end))

		TaskManager.Start()
	end)
end