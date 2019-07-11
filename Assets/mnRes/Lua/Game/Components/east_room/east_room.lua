--[[
    Author: xianbei
    Date: 2019-07-03 11:41:24
    LastEditors: xianbei
    LastEditTime: 2019-07-06 14:46:31
    Description: 东厢房
--]]


local east_room = Component('__east_room__', Config.ViewConstants.top, 'module/ui_east_room', 'Module')

function east_room:OnEnable()
    self.over_time = os.time() + 10

end

return east_room