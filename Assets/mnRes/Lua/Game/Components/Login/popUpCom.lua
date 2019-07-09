local popUpCom = {}

popUpCom.args = {
	btnOpen = Types.Button,
	btnClose = Types.Button,
}

local iVertical           				--是否水平布局
local iHorizontal         				--是否垂直布局
local _sOpenState = "normal"             --弹出框状态
--[[
	openState    
	"normal":不变化  
	"largen",:变大 
	"shrink":变小
]]--

local args = popUpCom.args
local _isSpringbackState = false
local _vec3Scale = self.gameObject.transform.localScale
local _iChangeTime = 0.1
local _funCallBack
function popUpCom:Awake()
	print("我是标题!!!!!!!!")
end

function popUpCom:Start()
	args.btnOpen.onClick:AddListener()
	args.btnClose.onClick:AddListener()
end

function openView(callback)
	if callback then
		_funCallBack = callback
	end
	openState = "largen"
	if iVertical == 1 and iHorizontal == 1 then
		_vec3Scale.localScale = Vector3(0,0,1)
	else
		_vec3Scale.localScale = Vector3(iVertical,iVertical,1)
	end
	_isSpringbackState = false
end

function closeView(callback)
	if callback then
		_funCallBack = callback
	end
	openState = "shrink"
end


function popUpCom:Update()
	
	if _sOpenState == "normal" then
		return
	end

	if _sOpenState == "largen" then
		--垂直变化
		if iVertical == 1 then
			if _vec3Scale.y >=1.1 or _isSpringbackState == true then
				_vec3Scale = _vec3Scale - Vector3(0,(UnityEngine.Time.deltaTime * 1.2 / _iChangeTime),0)
				_isSpringbackState = true
				if _vec3Scale.y <= 1 then
					_vec3Scale = Vector3( 1, 1, 1)
					_sOpenState = "normal"
					if _funCallBack then
						_funCallBack()
					end
				end
			elseif _isSpringbackState == false then
				_vec3Scale = _vec3Scale + Vector3(0,(UnityEngine.Time.deltaTime * 1.2 / _iChangeTime),0)
			end
		end
		--水平变化
		if iHorizontal == 1 then
			if _vec3Scale.x >=1.1 or _isSpringbackState == true then
				_vec3Scale = _vec3Scale - Vector3((UnityEngine.Time.deltaTime * 1.2 / _iChangeTime),0,0)
				_isSpringbackState = true
				if _vec3Scale.x <= 1 then
					_vec3Scale = Vector3( 1, 1, 1)
					_sOpenState = "normal"
					if _funCallBack then
						_funCallBack()
					end
				end
			elseif _isSpringbackState == false then
				_vec3Scale = _vec3Scale + Vector3((UnityEngine.Time.deltaTime * 1.2 / _iChangeTime),0,0)
			end
		end
	
	elseif _sOpenState == "shrink" then
		if iVertical == 1 then
			_vec3Scale = _vec3Scale - Vector3(0,(UnityEngine.Time.deltaTime / _iChangeTime),0)
			if _vec3Scale.y <=0 then
				_vec3Scale = Vector3( 0, 0, 1)
				_sOpenState = "normal"
				if _funCallBack then
					_funCallBack()
				end
			end
		end

		if iHorizontal == 1 then
			_vec3Scale = _vec3Scale - Vector3((UnityEngine.Time.deltaTime / _iChangeTime), 0, 0)
			if _vec3Scale.x <= 0 then
				_vec3Scale = Vector3( 0, 0, 1)
				_sOpenState = "normal"
				if _funCallBack then
					_funCallBack()
				end
			end
		end
	end
end

function popUpCom:OnEnable()

end

function popUpCom:OnDisable()
end

return popUpCom