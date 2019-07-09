Schedule = {}

local running = {}
local addMap = {}
local removedMap={}

local idGen = 0

function Schedule.Add(handler)
    idGen = idGen + 1
    addMap[idGen] = handler

    return idGen
end

function Schedule.Remove(key)
    if not key then
         return
    end

    table.insert(removedMap, key)
end

function Schedule.Update()
    for k,v in pairs(addMap) do
        running[k] = v
    end

    addMap = {}

    if #removedMap > 0 then
        for _, v in ipairs(removedMap) do
            running[v] = nil
        end

        removedMap = {}
    end

    for k, v in pairs(running) do
        v()
    end
end
