--[[
    Author: xianbei
    Date: 2019-07-03 11:41:24
    LastEditors: xianbei
    LastEditTime: 2019-07-06 11:44:35
    Description: 府宅UI
--]]

local ui_home = lua_class(base_view)

ui_home.args = {
    btn_main = Types.Button,
    btn_store = Types.Button,
    btn_study = Types.Button,
    btn_west = Types.Button,
    btn_east = Types.Button,
    btn_kitchen = Types.Button,
    img_head = Types.RawImage,
    node_bar = Types.GameObject
}

local args = ui_home.args

function ui_home:Awake( )
    -- body
    args.btn_main.onClick:AddListener(function ()
        Game.main_room:Active();
    end)
    args.btn_store.onClick:AddListener(function ()
        Game.store_room:Active();
    end)
    args.btn_study.onClick:AddListener(function ()
        Game.study_room:Active();
    end)
    args.btn_west.onClick:AddListener(function ()
        Game.west_room:Active();
    end)
    args.btn_east.onClick:AddListener(function ()
        Game.east_room:Active();
    end)
    args.btn_kitchen.onClick:AddListener(function ()
        Game.kitchen:Active();
    end)
    -- self.args.node_bar:SetActive(false);
end

function ui_home:OnEnable()
    self.super:OnEnable()
    -- Env.Persistence.test_data = {
    --     var1 = "xianbei",
    --     var2 = 1987,
    --     var3 = { 1, 2, 3, 4 },
    --     var4 = { a = 1, b = 2, c = 3, d = 4},
    --     var5 = { a = 1, b = "xianbei", c = { 4, 5, 6 }, d = {aa = 1, bb = 4, cc = 14} }
    -- }
    -- Env.Persistence.Save(test_data)
    print(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")
    dump(Env.Persistence.test_data)
end

return ui_home