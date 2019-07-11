if not Game then

    require 'Config.Config'
    require 'Base.Base'
    require 'Framework.Framework'
    require 'Game.Constants.Constants'

    print("Welcome to enter Lua World")

    rapidjson = require('rapidjson')
    util = require('xlua.util')
    utils = require 'Game.Utils.utils'

    EventManager = Event.New('game')

    ViewManager.Initialize()
    TaskManager.Initialize()
    CronManager.Initialize()

    function Shutdown()
        CronManager.Shutdown()
        TaskManager.Shutdown()
        ViewManager.Shutdown()
    end

    CoroutineManager:StartupEnv('Schedule', 'Update')

    base_view = require 'Game.base_view'

    Game = require 'Game.Game'

    Game:Active()

else
    print("Main.lua re-enter")
end
