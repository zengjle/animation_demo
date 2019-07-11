--[[
    Author: xianbei
    Date: 2019-07-02 15:48:16
    LastEditors: xianbei
    LastEditTime: 2019-07-06 15:33:45
    Description: UI基类
--]]


local base_view = {
    args = {
        btn_back = Types.Button
    },
    controller = {},
}


function base_view:Awake()
    print("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! base_view:Awake")
    self.args.btn_back.onClick:AddListener(function ()
        print("XXXXXXXXXXXXXXX close")
        self.controller:Deactive()
    end)
end

function base_view:Start()
    print("------------- base_view:Start")
end

function base_view:OnEnable()
    print("------------- base_view:OnEnable")
end

function base_view:OnDisable()
    print("------------- base_view:OnDisable")
end

function base_view:set_args(_tbl)
    table.merger(self.args,_tbl)
end


return base_view