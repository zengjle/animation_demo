--[[
游戏常量定义
]]

Constants = {}

-- 游戏版本号
Constants.gameVersion = '0.0.1'

-- 验证码
Constants.authKey = 'xxddffgg'

-- 登录服务器列表
Constants.loginServers = {
	{ip = '127.0.0.1', port = 8800},
}

require 'Game.Constants.Misc'
require 'Game.constants.Version'