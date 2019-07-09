--[[
游戏运行中环境数据
]]

Env = {}

function Env.Initialize()
	print('[Env] initialize')

	Env.RuntimeVars.Initialize()
	Env.Persistence.Initialize()
end

require 'Game.Components.Shared.Env.RuntimeVars'
require 'Game.Components.Shared.Env.Persistence'