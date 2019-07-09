--[[
    waiting菊花
]]

local Waiting = Component('__waiting__', Config.ViewConstants.top, 'Shared/Waiting/WaitingView', 'Cover')

function Waiting:Hide()
    Game.Shared.Waiting:Deactive(true)
end
return Waiting