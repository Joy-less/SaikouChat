extends Node

@export_file("*.gguf") var model_path:String
@export var thread_count:int = 1

var mutex := Mutex.new()

func generate_async(prompt:String, grammar:String, json:String, on_partial:Callable) -> String:
	# Setup generator
	var generator := GDLlama.new()
	generator.model_path = model_path
	generator.should_output_prompt = false
	generator.should_output_special = false
	generator.context_size = len(prompt)
	generator.n_threads = thread_count
	add_child(generator)
	# Connect partial generate callback
	if on_partial:
		generator.generate_text_updated.connect(on_partial)
	# Generate result
	generator.run_generate_text(prompt, grammar, json)
	var result:String = await generator.generate_text_finished
	# Disconnect partial generate callback
	if on_partial:
		generator.generate_text_updated.disconnect(on_partial)
	# Dispose generator
	generator.queue_free()
	# Return result
	return result

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
	
	# Generate result
	var result = JSON.parse_string(await generate_async(prompt, "", schema, on_partial))
	if not result:
		return ""
	return result["response"]
