extends RefCounted

signal task_completed(result: Variant)

func call_async(target: Object, method: String, args: Array = []) -> void:
    var result: Variant = await target.callv(method, args)
    task_completed.emit(result)