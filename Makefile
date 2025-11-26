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
		--global-property models \
		--additional-properties=library=unityWebRequest
	@echo "Copying generated models..."
	@if [ -d "$(TEMP_DIR)/src/LudiscanApiClient.Runtime.ApiClient.Dto/Model" ]; then \
		cp -rf $(TEMP_DIR)/src/LudiscanApiClient.Runtime.ApiClient.Dto/Model/*.cs $(DTO_OUTPUT_DIR)/; \
		echo "DTO classes generated successfully in $(DTO_OUTPUT_DIR)"; \
	else \
		echo "Warning: Generated model directory not found"; \
		find $(TEMP_DIR) -name "*.cs" -path "*/Model/*" 2>/dev/null | head -20; \
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

# Help
help:
	@echo "Ludiscan Unity API Client - Build Commands"
	@echo ""
	@echo "Usage:"
	@echo "  make gen-dto              Generate DTO classes from local API server"
	@echo "  make gen-dto-url URL=...  Generate DTO classes from custom Swagger URL"
	@echo "  make clean                Clean temporary files"
	@echo ""
	@echo "Note: The API client uses UnityWebRequest (not RestSharp)."
	@echo "      Only DTO classes are auto-generated from Swagger."
