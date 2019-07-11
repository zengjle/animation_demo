--[[
本地数据持久化
]]

local Persistence = {}

-- 账号相关数据
Persistence.account = {}

-- 最近一次选择的服务器相关数据
Persistence.lastServer = {}

Persistence.test_data = {}

function Persistence.Initialize()
	print('[Persistence] initialize')

	for k, v in pairs(Persistence) do 
		if type(v) == 'table' then
			Persistence.load(k)
		end
	end
end

-- 保存数据，如果有指定模块名，就只保存模块的数据，否则就全部保存
function Persistence.Save(m)
	for k, v in pairs(Persistence) do
		if type(v) == 'table' and (not m or (m and m == k)) then
			local str = rapidjson.encode(v and v or {})
			print('[Persistence] save ' .. k .. ', ' .. str)
			UnityEngine.PlayerPrefs.SetString(k, str)

			-- 如果是保存特定模块，完成后直接退出
			if m and m == k then
				break
			end
		end			
	end
end

-- 获取本地持久化数据
function Persistence.load(name)
	local v = UnityEngine.PlayerPrefs.GetString(name)
	print('[Persistence] load ' .. name .. ' valued ' .. ((v and #v > 0) and v or 'undefined'))
	Persistence[name] = v and rapidjson.decode(v) or {}
end

Env.Persistence = Persistence

