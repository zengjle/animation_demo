--[[
cron系统
]]

local cron = require '3rd.cron'

CronManager = {}

local self = CronManager

self.crons = {}
self.co = nil

function CronManager.Initialize()
    print('[CronManager] initialize')
    self.co = Coroutine.Start(self.update)
end

function CronManager.Shutdown()
    if self.co then
        Coroutine.Stop(self.co)
        self.co = nil
    end

    print('[CronManager] shutdown')
end

function CronManager.update()
    while true do
        local interval = TimeManager.interval

        for k, v in pairs(self.crons) do
            if v:update(interval) then
                self.crons[k] = nil
            end
        end

        Coroutine.Step()
    end
end

-- 几秒钟后执行1次
function CronManager.After(name, second, handler, ...)
    self.crons[name] = cron.after(second, handler, ...)
end

-- 每隔几秒执行1次
function CronManager.Every(name, second, handler, ...)
    self.crons[name] = cron.every(second, handler, ...)
end

-- 删除
function CronManager.Remove(name)
    if not self.crons[name] then
        return
    end

    self.crons[name] = nil
end