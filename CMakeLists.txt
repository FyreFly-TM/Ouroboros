cmake_minimum_required(VERSION 3.20)
project(Ouro VERSION 1.0.0 LANGUAGES CXX CSharp)

# Set C++ standard
set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Set CSharp language version
set(CMAKE_CSharp_FLAGS "/langversion:latest")

# Options
option(BUILD_TESTS "Build test suite" ON)
option(BUILD_DOCS "Build documentation" ON)
option(BUILD_TOOLS "Build development tools" ON)
option(BUILD_LSP "Build language server" ON)
option(ENABLE_JIT "Enable JIT compilation" ON)
option(ENABLE_GPU "Enable GPU support" OFF)

# Find .NET SDK
find_program(DOTNET dotnet)
if(NOT DOTNET)
    message(FATAL_ERROR "dotnet CLI not found. Please install .NET SDK 6.0 or later.")
endif()

# Output directories - all build artifacts go to build/
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_SOURCE_DIR}/build/bin)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${CMAKE_SOURCE_DIR}/build/lib)
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${CMAKE_SOURCE_DIR}/build/lib)

# Add subdirectories
add_subdirectory(src)

if(BUILD_TESTS)
    enable_testing()
    add_subdirectory(tests)
endif()

if(BUILD_TOOLS)
    add_subdirectory(tools)
endif()

if(BUILD_DOCS)
    add_subdirectory(docs)
endif()

# Generate version header
configure_file(
    "${CMAKE_CURRENT_SOURCE_DIR}/src/Version.cs.in"
    "${CMAKE_CURRENT_SOURCE_DIR}/build/generated/Version.cs"
    @ONLY
)

# Installation
install(FILES README.md LICENSE DESTINATION .)

# CPack configuration
set(CPACK_PACKAGE_NAME "Ouro")
set(CPACK_PACKAGE_VERSION ${PROJECT_VERSION})
set(CPACK_PACKAGE_DESCRIPTION "Multi-paradigm programming language")
set(CPACK_PACKAGE_VENDOR "Ouro Project")
set(CPACK_GENERATOR "ZIP;TGZ")
include(CPack)

# Summary
message(STATUS "")
message(STATUS "Ouro Configuration Summary:")
message(STATUS "  Version:        ${PROJECT_VERSION}")
message(STATUS "  Build Type:     ${CMAKE_BUILD_TYPE}")
message(STATUS "  Install Prefix: ${CMAKE_INSTALL_PREFIX}")
message(STATUS "  Build Tests:    ${BUILD_TESTS}")
message(STATUS "  Build Docs:     ${BUILD_DOCS}")
message(STATUS "  Build Tools:    ${BUILD_TOOLS}")
message(STATUS "  Build LSP:      ${BUILD_LSP}")
message(STATUS "  Enable JIT:     ${ENABLE_JIT}")
message(STATUS "  Enable GPU:     ${ENABLE_GPU}")
message(STATUS "") 