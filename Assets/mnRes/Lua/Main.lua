if not Game then

    require 'Config.Config'
    require 'Base.Base'
    require 'Framework.Framework'
    require 'Game.Constants.Constants'

    print("Welcome to enter Lua World")

    rapidjson = require('rapidjson')
    util = require('xlua.util')

    EventManager = Event.New('game')

    ViewManager.Initialize()
    TaskManager.Initialize()

    function Shutdown()
        TaskManager.Shutdown()
        ViewManager.Shutdown()
    end

    CoroutineManager:StartupEnv('Schedule', 'Update')

    Game = require 'Game.Game'

    Game:Active()

else
    print("Main.lua re-enter")
end
