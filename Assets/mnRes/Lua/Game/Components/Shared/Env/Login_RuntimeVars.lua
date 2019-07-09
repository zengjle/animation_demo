--[[
登录相关的运行期数据
]]

local Login_RuntimeVars = {}

-- 服务器列表
Login_RuntimeVars.serverlist = {}

-- 登录时的公告信息
Login_RuntimeVars.notices = {}

-- 大厅服务器ip、功能端口、聊天端口
Login_RuntimeVars.lobbyServer = {ip = '', port = 1234, chatPort = 2345}

-- 导出模块
Env.RuntimeVars.login = Login_RuntimeVars
