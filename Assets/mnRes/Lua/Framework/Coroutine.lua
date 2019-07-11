--[[
协程
]]

Coroutine = {}

local create = coroutine.create
local resume = coroutine.resume
local yield = coroutine.yield
local schedule_add = Schedule.Add
local schedule_remove = Schedule.Remove

local State_Stop = -1
local State_Normal = 0
local State_WaitTime = 1
local State_Until = 2
local State_While = 3
local State_WWW = 4
local State_Finish = 5

local table_insert = table.insert
local trace_error = error
local debug_traceback = debug.traceback

local curCoroutineID = nil

local idGen = 0
local updateKey = -1

local pool = {}
local addPool = nil
local removePool = nil

local Time = TimeManager

local function update()
    local count = 0

    if addPool then
        if removePool then
            for k,v in pairs(addPool) do
                if not removePool[k] then pool[k] = v end
            end
        else
            for k,v in pairs(addPool) do
                pool[k] = v
            end
        end

        addPool = nil
    end

    for k,v in pairs(pool) do
        if not removePool or not removePool[k] then
            curCoroutineID = k

            local state = v.state

            local ret = false
            local err = nil

            if state == State_Normal then
                v.state = State_Finish
                ret, err = resume(v.func)
            elseif state == State_WaitTime then
                if Time.seconds >= v.args then
                    v.state = State_Finish
                    v.args = nil
                    ret,err = resume(v.func)
                else
                    ret = true
                end
            elseif state == State_Until then
                if v.args() then
                    v.state=State_Finish
                    v.args = nil
                    ret,err = resume(v.func)
                else
                    ret = true
                end
            elseif state == State_While then
                if not v.args() then
                    v.state = State_Finish
                    v.args = nil
                    ret,err = resume(v.func)
                else
                    ret = true
                end
            elseif state == State_WWW then
                if v.args.isDone then
                    v.state = State_Finish
                    v.args = nil
                    ret,err = resume(v.func)
                else
                    ret = true
                end
            end

            if not ret then
                Coroutine.Stop(k)

                if err then
                    error(tostring(err) .. '  ' .. debug_traceback(v.func))
                end
            end

            count = count+1
        end
    end

    if removePool then
        for k,v in pairs(removePool) do
            pool[k] = nil
        end

        removePool = nil
    end

    if count == 0 and addPool == nil then
        schedule_remove(updateKey)
        updateKey = -1
    end
end

function Coroutine.Start(func)
    idGen = idGen + 1

    if not addPool then
        addPool = {}
    end

    addPool[idGen] = { func = create(func), state = State_Normal }

    if updateKey == -1 then
        updateKey = schedule_add(update)
    end

    return idGen
end

function Coroutine.Stop(key)
    if not key then
        return
    end

    if not removePool then
        removePool = {}
    end

    removePool[key] = true

    if updateKey == -1 then
        updateKey = schedule_add(update)
    end
end

function Coroutine.Wait(time)
    local val = pool[curCoroutineID]

    val.state = State_WaitTime
    val.args = time + Time.seconds

    yield()
end

function Coroutine.Step()
    local val = pool[curCoroutineID]

    val.state = State_Normal
    val.args = nil

    yield()
end

function Coroutine.Until(handler)
    local val = pool[curCoroutineID]

    val.state = State_Until
    val.args = handler

    yield()
end

function Coroutine.While(handler)
    local val = pool[curCoroutineID]

    val.state = State_While
    val.args = handler

    yield()
end

function Coroutine.WWW(www)
    local val = pool[curCoroutineID]

    val.state = State_WWW
    val.args = www

    yield()
end

function Coroutine.ExecuteInWaitFrame(func, n)
    Coroutine.Start(function()
        local num = (n and n > 0) and n or 1
        while num > 0 do
            Coroutine.Step()
            num = num - 1
        end

        if func then func() end
    end)
end