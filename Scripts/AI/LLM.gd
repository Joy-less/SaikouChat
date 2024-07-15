extends Node

@export_file("*.gguf") var model_path:String

func generate_async(prompt:String, grammar:String, json:String, on_partial:Callable) -> String:
	# Setup generator
	var generator := GDLlama.new()
	generator.model_path = model_path
	generator.should_output_prompt = false
	generator.should_output_special = false
	add_child(generator)
	# Connect partial generate callback
	if on_partial != null:
		generator.generate_text_updated.connect(on_partial)
	# Generate result
	generator.context_size = len(prompt)
	generator.run_generate_text(prompt, grammar, json)
	var result:String = await generator.generate_text_finished
	# Disconnect partial generate callback
	if on_partial != null:
		generator.generate_text_updated.disconnect(on_partial)
	# Dispose generator
	generator.queue_free()
	# Return result
	return result

func stop_generate() -> void:
	for child in get_children():
		if child is GDLlama:
			child.stop_generate_text()

func prompt_async(prompt:String, min_length:int, max_length:int, on_partial:Callable) -> String:
	var schema:String = """
		{
			"type": "object",
			"properties": {
				"response": {
					"type": "string",
					"minLength": {min_length},
					"maxLength": {max_length}
				}
			},
			"required": ["response"]
		}
		""".format({"min_length": min_length, "max_length": max_length})
	
	# Create result schema
	#var schema:String = """
	#	{
	#		"type": "string",
	#		"minLength": {min_length},
	#		"maxLength": {max_length}
	#	}
	#	""".format({"min_length": min_length, "max_length": max_length})
	
	# Generate result
	var result = JSON.parse_string(await generate_async(prompt, "", schema, on_partial))
	if result == null:
		return ""
	return result.response
