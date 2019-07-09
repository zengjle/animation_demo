--[[
事件类型定义
]]

EventTypes = {}
EventTypes.descs = {}

function event(et, desc)
	if EventTypes[et] then
		error('[EventTypes] event type {' .. et .. '} has exist')
		return
	end

	if not desc or #desc == 0 then
		error('[EventTypes] event ' .. et .. ' need description for using' )
		return
	end

	EventTypes[et] = et
	EventTypes.descs[et] = desc
end

event('System_TaskProgress', 				'任务系统进度有更新时触发')
event('System_SceneLoadFinished', 			'场景载入切换成功后触发')
event('System_BeforeContextSwitched', 		'当context准备切换之前触发')
event('System_ContextSwitched', 			'当context切换时触发')
event('System_AllSignalsFinish', 			'当所有的信号量完成时触发')
event('System_ResponseError', 				'当网络消息返回error时')
event('System_ResponseFatal', 				'当网络消息返回fatal时')
event('System_ResponseDelay',				'当网络消息返回延迟时')
event('System_ResponseSuccess',				'当网络消息返回成功')
event('System_IdentifySuccess',				'网络身份校验成功')
event('System_ResendError', 				'当消息重发失败时')
event('System_ConnectError', 				'网络请求连接失败')
event('System_Disconnect', 					'网络断开连接时')
event('System_ViewActive',					'当view被激活时')
event('System_ViewInFocus',					'当view获得焦点时')
event('System_ViewOutFocus',				'当view失去焦点时')