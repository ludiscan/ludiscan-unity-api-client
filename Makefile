# Ludiscan Unity API Client - Makefile
#
# This Makefile generates DTO (Data Transfer Object) classes from Swagger/OpenAPI specification.
# The API client uses UnityWebRequest and does not depend on RestSharp.
#
# Prerequisites:
#   - openapi-generator-cli (npm install @openapitools/openapi-generator-cli -g)
#   - Node.js/npm
#
# Usage:
#   make gen-dto         - Generate DTO classes from local API server
#   make gen-dto-url URL=https://your-api.com/swagger/json - Generate from custom URL
#   make clean           - Clean generated files

# Default API URL (local development server)
SWAGGER_URL ?= http://localhost:3211/swagger/api/v0/json

# Output directories
TEMP_DIR := Temp/Api
DTO_OUTPUT_DIR := Assets/Matuyuhi/LudiscanApiClient/Runtime/ApiClient/Dto/Generated

.PHONY: gen-dto gen-dto-url clean help

# Generate DTO classes from Swagger specification
gen-dto:
	@echo "Generating DTO classes from $(SWAGGER_URL)..."
	@mkdir -p $(DTO_OUTPUT_DIR)
	openapi-generator-cli generate \
		-i $(SWAGGER_URL) \
		-g csharp \
		-o $(TEMP_DIR) \
		-c api-generate-config.json \
		--global-property models,modelDocs=false,modelTests=false
	@echo "Copying and fixing generated models..."
	@FOUND_DIR=$$(find $(TEMP_DIR) -type d -name "Model" | head -1); \
	if [ -n "$$FOUND_DIR" ] && [ -d "$$FOUND_DIR" ]; then \
		echo "Found model directory: $$FOUND_DIR"; \
		for f in $$FOUND_DIR/*.cs; do \
			if [ -f "$$f" ]; then \
				filename=$$(basename "$$f"); \
				echo "  Processing: $$filename"; \
				sed -i \
					-e '/using FileParameter/d' \
					-e '/using OpenAPIDateConverter/d' \
					-e 's/namespace LudiscanApiClient\.Runtime\.ApiClient\.Dto\.Model/namespace LudiscanApiClient.Runtime.ApiClient.Dto.Generated/' \
					"$$f" 2>/dev/null || true; \
				cp -f "$$f" $(DTO_OUTPUT_DIR)/; \
			fi \
		done; \
		echo "DTO classes generated successfully in $(DTO_OUTPUT_DIR)"; \
	else \
		echo "Warning: Generated model directory not found"; \
		echo "Searching for .cs files..."; \
		find $(TEMP_DIR) -name "*.cs" 2>/dev/null | head -20; \
	fi

# Generate DTO classes from custom URL
gen-dto-url:
	@echo "Generating DTO classes from $(URL)..."
	$(MAKE) gen-dto SWAGGER_URL=$(URL)

# Clean temporary files
clean:
	@echo "Cleaning temporary files..."
	rm -rf $(TEMP_DIR)
	@echo "Clean complete"

# Clean generated DTOs
clean-generated:
	@echo "Cleaning generated DTO files..."
	rm -rf $(DTO_OUTPUT_DIR)/*.cs
	@echo "Clean complete"

# Help
help:
	@echo "Ludiscan Unity API Client - Build Commands"
	@echo ""
	@echo "Usage:"
	@echo "  make gen-dto              Generate DTO classes from local API server (localhost:3211)"
	@echo "  make gen-dto-url URL=...  Generate DTO classes from custom Swagger URL"
	@echo "  make clean                Clean temporary files"
	@echo "  make clean-generated      Clean generated DTO files"
	@echo ""
	@echo "Prerequisites:"
	@echo "  npm install @openapitools/openapi-generator-cli -g"
	@echo ""
	@echo "Note: The API client uses UnityWebRequest (not RestSharp)."
	@echo "      Only DTO/Model classes are auto-generated from Swagger."
	@echo "      Generated files go to: $(DTO_OUTPUT_DIR)"
