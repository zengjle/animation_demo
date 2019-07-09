--[[
this file is auto-generated by Configer Tools
]]

Config = Config or {}
local Config = Config
local c = Config


local setmetatable=setmetatable
local function i(targetTable,keyList,valueList) for it=1,#keyList do targetTable[keyList[it]]=valueList[it] end end
local function getConfigByID(self,id) for it=1,#(self.configs) do if self.configs[it].id == id then return self.configs[it] end end end
local function getConfigByName(self,name) for it=1,#(self.configs) do if self.configs[it].name == name then return self.configs[it] end end end
local function cet(count) local ret={} for it=1,count do ret[it]={} end return ret end
local function fillConfigData(table,keyList,valueList) for it=1,#table do i(table[it],keyList,valueList[it]) end end

local configMetatable=
{
    __index=function(t,k) if type(k) == 'number' then return t:getConfigByID(k) else return t:getConfigByName(k) end end,
    __newindex=function(t,k) print('config is readonly,do not modify') end,
}
local function b(count) return setmetatable({configs = cet(count),getConfigByID=getConfigByID,getConfigByName=getConfigByName},configMetatable) end


c.ViewLayer = b(5)



--ViewConstants
c.ViewConstants = require 'Config.Db_ViewConstants'
c.utils = require 'Config.utilsss'
--ViewLayer
fillConfigData(c.ViewLayer.configs,table.unpack(require 'Config.Db_ViewLayer'))


return Config