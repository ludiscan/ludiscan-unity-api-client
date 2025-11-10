# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2024-11-10

### Added
- Initial release of Ludiscan API Client for Unity
- Position logging functionality for player tracking
- Session management with automatic tracking
- Event logging for custom game events
- Field object state tracking
- Pre-built OpenAPI-generated C# client DLL
- All necessary dependencies bundled (RestSharp, Polly, Newtonsoft.Json)
- Comprehensive documentation and examples
- Support for both 2D and 3D position data
- Async/await support for all API operations
- Project-based data organization

### Technical Details
- Targets Unity 2022.2+
- Uses RestSharp for HTTP communication
- Uses Polly for resilience and retry policies
- Uses Newtonsoft.Json for JSON serialization
- Assembly Definition support for proper dependency management

## [Unreleased]

### Planned Features
- Batch position logging for improved performance
- Local caching with offline support
- Automatic reconnection on network failure
- Performance metrics and diagnostics
- Analytics dashboard integration

## Notes

### Known Limitations
- Requires Ludiscan backend API to be running
- Position logging frequency should be managed by application to avoid server overload
- Uses RestSharp v107.3.0 (has known moderate severity vulnerability - consider upgrading)

### Migration Guide

If you were using an older version, refer to this section for upgrading instructions.

### Support

For support and bug reports, please visit:
- GitHub Issues: https://github.com/ludiscan/ludiscan-unity-api-client/issues
- Documentation: https://ludiscan.example.com/docs
