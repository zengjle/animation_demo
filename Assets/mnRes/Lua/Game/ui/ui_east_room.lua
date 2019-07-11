--[[
    Author: xianbei
    Date: 2019-07-03 11:41:24
    LastEditors: xianbei
    LastEditTime: 2019-07-06 15:43:36
    Description: 东厢房UI
--]]


local ui_east_room = lua_class(base_view)

ui_east_room.args = {
    btn_back = Types.Button,
    btn_cd = Types.Button,
    btn_ci = Types.Button,
    txt_cd = Types.Text,
    edt_name = Types.InputField,
    txt_res = Types.Text,
}

local update_cd = function()
    local txt_cd = ui_east_room.args.txt_cd
    local over_time = ui_east_room.controller.over_time
    local cd_time = over_time - os.time()
    if cd_time > 0 then
        txt_cd.text = utils.get_time_string(cd_time)
    else
        txt_cd.text = "征收"
        CronManager.Remove('test1')
        ui_east_room.args.btn_cd.interactable = true
    end

end

function ui_east_room:Awake()
    self.super:Awake()
    self.args.btn_back.onClick:AddListener( function ()
        self.controller:Deactive()
    end)
    self.args.btn_cd.onClick:AddListener( function()
        self.controller.over_time = os.time() + 10
        self.args.txt_cd.text = utils.get_time_string(10)
        CronManager.Every('test1', 1, update_cd)
        self.args.btn_cd.interactable = false
        db.res.ingot = db.res.ingot + 10
        self.args.txt_res.text =  db.res.ingot
    end)
    self.args.btn_ci.onClick:AddListener( function()
        db.nick_name = self.args.edt_name.text
    end)

end



function ui_east_room:OnEnable()

    self.super:OnEnable()        
    CronManager.Every('test1', 1, update_cd)

    self.args.btn_cd.interactable = false

    self.args.edt_name.text = db.nick_name
    self.args.txt_res.text =  db.res.ingot

end

function ui_east_room:OnDisable()
    EventManager:Broadcast("db_update")
    Env.Persistence.Save()
end



return ui_east_room