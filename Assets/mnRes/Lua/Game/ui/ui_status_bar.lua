--[[
    Author: xianbei
    Date: 2019-07-03 11:41:24
    LastEditors: xianbei
    LastEditTime: 2019-07-06 15:40:25
    Description: 顶部信息栏，底部菜单栏
--]]

local ui_status_bar = {}

ui_status_bar.args = {
    btn_player = Types.Button,
    btn_mall = Types.Button,
    btn_recharge = Types.Button,
    btn_buddy = Types.Button,
    btn_card = Types.Button,
    btn_bag = Types.Button,
    btn_task = Types.Button,
    btn_archive = Types.Button,
    btn_gate = Types.Button,
    txt_nick = Types.Text,
    txt_res = Types.Text,
}
local _anirAnimator
local args = ui_status_bar.args
local _sBarAniState = "close"
function ui_status_bar:Awake( )
    _anirAnimator = self.gameObject:GetComponent("Animator")
    
    args.btn_player.onClick:AddListener(function ()
        _anirAnimator:Play("barFadeOut")
        -- Game.main_room:Active();
    end)

    args.btn_mall.onClick:AddListener(function ()
        _anirAnimator:Play("barFadeInt")
        -- Game.store_room:Active();
    end)
    args.btn_recharge.onClick:AddListener(function ()
        Game.study_room:Active();
    end)
    args.btn_buddy.onClick:AddListener(function ()
        Game.west_room:Active();
    end)
    args.btn_card.onClick:AddListener(function ()
        Game.east_room:Active();
    end)
    args.btn_bag.onClick:AddListener(function ()
        Game.kitchen:Active();
    end)
    args.btn_task.onClick:AddListener(function ()
        Game.kitchen:Active();
    end)
    args.btn_archive.onClick:AddListener(function ()
        Game.kitchen:Active();
    end)
    args.btn_gate.onClick:AddListener(function ()
        Game.kitchen:Active();
    end)

    EventManager:AddListener("db_update",function()
        print("db_update")
        self.args.txt_nick.text = db.nick_name
        self.args.txt_res.text = db.res.ingot
    end)
    self.args.txt_nick.text = db.nick_name
    self.args.txt_res.text = db.res.ingot
end

function ui_status_bar:barClose()
    _sBarAniState = "close"
    print("--------------_sBarAniState :close")
end

function ui_status_bar:barOpen()
    _sBarAniState = "open"
end

function ui_status_bar:playEnd()
    print("status_bar is ".._sBarAniState)
end

return ui_status_bar