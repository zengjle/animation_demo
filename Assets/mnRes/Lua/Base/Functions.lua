--[[
定义一些常用的函数
]]

local rawPrint = print
local consoleFormat = {"<color=#C64E00>", "source", ":", "line", "</color> ", "val"}
local logger = {UnityEngine.Debug.Log, UnityEngine.Debug.Log, UnityEngine.Debug.LogWarning, UnityEngine.Debug.LogError}
local logLevel = 1
local printStack = false

-- 切换场景时不删除
function DontDestroyOnLoad(go)
	UObject.DontDestroyOnLoad(go)
end

-- 拆分字符串
function Split(str, filters)
	local ss = {}
	for s in string.gmatch(str, filters) do
		table.insert(ss, s)
	end

	return ss
end

-- 检查字符串是否以指定字符串开头
function string.starts(str, startStr)
	return string.sub(str, 1, #startStr) == startStr
end

-- 检查字符串是否以指定字符串结束
function string.ends(str, endStr)
	return endStr == '' or string.sub(str, -#endStr) == endStr
end

-- 打印日志
local function log2Console(logType, v, ...)
	if logLevel == 0 or logLevel > logType then
		return
	end

	local info = debug.getinfo(3)

	if string.ends(info.source, '.lua') then
		consoleFormat[2] = string.gsub(string.sub(info.source, 1, #info.source - 4), '[.]', '/') .. '.lua'
	else
		consoleFormat[2] = string.gsub(string.sub(info.source, 2), '[.]', '/') .. '.lua'
	end		
	
	consoleFormat[4] = info.currentline
	consoleFormat[6] = tostring(v)
	if printStack then 
		consoleFormat[6] = consoleFormat[6] .. '\n' .. debug.traceback()
	end

	logger[logType]('<color=#0099ff>[Lua]</color>' .. table.concat(consoleFormat))
	consoleFormat[6] = 'val'
end

-- 控制台日志信息
function print(v, ...)
	log2Console(1, v, ...)
end

-- 控制台里有效信息
function info(v, ...)
	log2Console(2, v, ...)
end

-- 控制台警告信息
function warn(v, ...)
	log2Console(3, v, ...)
end

-- 控制台错误信息
function error(v, ...)
	log2Console(4, v, ...)
end

-- 打印
function dump(v)
	local function traverse (t)
		local str = '{'

		local comma = false
		for k, v in pairs(t) do
			if comma then
				str = str .. ', '
			end

			if type(k) == 'number' then
				str = str .. '[' .. k .. ']='		
			else
				str = str .. k .. '='
			end

			if type(v) == 'table' then
				str = str .. traverse(v)
			else
				str = str .. tostring(v)
			end

			comma = true
		end

		str = str .. '}'

		return str
	end

	print('---------------- dump begin ------------------- \n' .. traverse(v))
end

-- 设置打印日志的等级
function setLogLevel(level)
	logLevel = level
	
	if level == 0 then
		UnityEngine.Debug.logger.logEnabled = false
	end
end

-- 打印时是否带lua堆栈
function setPrintStack(v)
	printStack = v
end

-- 去除文件扩展名
function string.stripExtension(str)
	local idx = str:match(".+()%.%w+$")
	return idx and str:sub(1, idx-1) or str
end

-- 深度拷贝
function deepCopy(t)
	local lookupTable = {}

	local function copy (t)
		if t == nil then
			return nil
		end

		if type(t) ~= 'table' then
			return t
		end

		local nt = {}
		lookupTable[t] = nt

		for k, v in pairs(t) do
			nt[copy(k)] = copy(v)
		end

		return setmetatable(nt, getmetatable(t))
	end

	return copy(t)
end

-- 深度拷贝
function table.clone(t)
	return deepCopy(t)
end

-- 简单点的表copy
function table.sclone(t)
	return {table.unpack(t)}
end

-- 合并表
function table.merge(d, s, notRecover)
	for k, v in pairs(s) do
		if not notRecover or (notRecover and not d[k]) then
			d[k] = v
		end
	end
end

-- CS协程
CSCoroutine = {}

CSCoroutine.Start = function (...)
	return CoroutineManager:StartCoroutine(util.cs_generator(...))
end

CSCoroutine.Stop = function (co)
	CoroutineManager:StopCoroutine(co)
end

function lua_class(_super,classname)
	local superType = type(_super)
	local cls,super
 
	if superType ~= "function" and superType ~= "table" then
		superType = nil
		super = nil
	elseif superType == "table" then
		super = table.clone(_super)
	end
 
	if superType == "function" or (super and super.__ctype == 1) then
		-- inherited from native C++ Object
		cls = {}
 
		if superType == "table" then
			-- copy fields from super
			for k,v in pairs(super) do cls[k] = v end
			cls.__create = super.__create
			cls.super    = super
		else
			cls.__create = super
			cls.ctor = function() end
		end
 
		cls.__cname = classname
		cls.__ctype = 1
 
		function cls.new(...)
			local instance = cls.__create(...)
			-- copy fields from class to native object
			for k,v in pairs(cls) do instance[k] = v end
			instance.class = cls
			instance:ctor(...)
			return instance
		end
 
	else
		-- inherited from Lua Object
		if super then
			cls = {}
			setmetatable(cls, {__index = super,  
				__newindex = function(cls, key, value)
					rawset(cls, key, value)
					if type(value) ~= "function" and super[key] ~= nil then
						rawset(super, key, value)
					end
			end})
			cls.super = super
		else
			cls = {ctor = function() end}
		end
 
		cls.__cname = classname
		cls.__ctype = 2 -- lua
		cls.__index = cls
 
		function cls.new(...)
			local instance = setmetatable({}, cls)
			instance.class = cls
			instance:ctor(...)
			return instance
		end
	end
 
	return cls
 end
