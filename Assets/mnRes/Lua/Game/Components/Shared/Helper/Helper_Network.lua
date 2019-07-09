--[[
网络相关的辅助功能
]]

local Connections = ConnectionManager

-- http请求
local startHttpRequest = function (servers, command, handler, timeout, demandDecode, id, retryCount, maxRetry, next, tag)
	local server = servers[id]
	local url = nil
	local coTimeout = nil

	if string.starts(server.ip, 'http:') then
		url = server.ip .. ':' .. server.port .. command
	else
		url = 'http://' .. server.ip .. ':' .. server.port .. command
	end
	
	if not retryCount or retryCount == 0 then
		print('[' .. tag .. '] start request ' .. url)
	else
		print('[' .. tag .. '] start request ' .. url .. ', retryCount ' .. retryCount)
	end
	
	local co = CoroutineManager:StartCoroutine(
		NetworkManager:HttpRequest(url, 
			function (msg)
				print('[' .. tag .. '] request ' .. command .. ', response ' .. msg)
				if handler and type(handler) == 'function' then
					handler(rapidjson.decode(msg))
				end

				if coTimeout then
					CSCoroutine.Stop(coTimeout)
					coTimeout = nil
				end
			end,

			function (err)
				if coTimeout then
					CSCoroutine.Stop(coTimeout)
					coTimeout = nil
				end

				if retryCount < maxRetry then
					retryCount = retryCount + 1
				else
					id = id + 1
					retryCount = 0
				end

				if id <= #servers then
					next(servers, command, handler, timeout, demandDecode, id, retryCount, maxRetry, next, tag)
				else
					error('[' .. tag .. '] request {' .. command .. '} failed, reason \"' .. err ..'\"')
				end
			end,

			demandDecode
		)
	)

	if timeout then
		coTimeout = CSCoroutine.Start(function ()
			coroutine.yield(Delay.wait3Seconds)
			
			print('[' .. tag .. '] request ' .. command .. ' timeout')

			if co then
				CoroutineManager:StopCoroutine(co)
			end

			co = nil

			CSCoroutine.Stop(coTimeout)
			coTimeout = nil
		end)
	end
end

-- 向入口服务器发送请求
Helper.Request2EntrySever = function (command, handler)
	startHttpRequest(Constants.entryServers, command, handler, false, false, 1, 0, 2, startHttpRequest, 'Request2EntrySever')
end

-- 向大厅服务器的web service发送请求
Helper.Request2LobbyServerWS = function (command, handler)
	local lastServer = Env.Persistence.lastServer
	if not lastServer.out_game_ip then
		warn('[Request2LobbyServerWS] last login server is invalid')
		return
	end

	local servers = {
		{ip = lastServer.out_game_ip, port = lastServer.http_port}
	}

	startHttpRequest(servers, command, handler, false, true, 1, 0, 2, startHttpRequest, 'Request2LobbyServerWS')
end

-- 向大厅服务器发送请求
Helper.Request2LobbyServer = function (command, args, handler, failHandler, taskMode)
	print('[Request2LobbyServer] start command ' .. command)
	local link = Connections.lobbyConn
	if not link then
		warn('[Request2LobbyServer] lobby connection is invalid')
		return
	end

	if not PacketTypes.id2infos[tostring(command)] then
		warn('[Request2LobbyServer] command ' .. command .. ' is invalid, confirm pls')
		return
	end

	args = args and args or {}

	local localPlayer = Env.LocalPlayer
	if localPlayer and localPlayer.base and localPlayer.base.sid then
		args.sid = localPlayer.base.sid
		print('------------------------- sid ' .. args.sid)
	end

	link:send(command, args, handler, failHandler, taskMode)
end

-- 监听从lobby服务器主动发来的消息包
Helper.RegisterListener4LobbyServer = function (command, handler)
	local link = Connections.lobbyConn
	if not link then
		warn('[RegisterListener4LobbyServer] lobby connection is invalid')
		return
	end
	
	link:addListener(command, handler)
end
