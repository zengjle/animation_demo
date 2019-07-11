--[[
    Author: xianbei
    Date: 2019-07-03 11:41:24
    LastEditors: xianbei
    LastEditTime: 2019-07-06 15:02:22
    Description: 府宅
--]]


local home = Component('__home__', Config.ViewConstants.top, 'base/ui_home', 'Base')

function home:OnEnable()
    db = Env.Persistence.test_data
    if db.nick_name == nil then
        db.nick_name = 'nick_name'
    end
    if db.res == nil then
        db.res = {}
        db.res.ingot = 100
    end
end

return home