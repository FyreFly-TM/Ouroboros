# Ouroboros Test Suite

file(GLOB_RECURSE TEST_SOURCES
    "*.cs"
)

add_executable(OuroborosTests ${TEST_SOURCES})
target_link_libraries(OuroborosTests OuroborosCore)

set_property(TARGET OuroborosTests PROPERTY VS_DOTNET_TARGET_FRAMEWORK_VERSION "v6.0")
set_property(TARGET OuroborosTests PROPERTY VS_GLOBAL_LangVersion "latest")

# Add test command
add_test(NAME OuroborosTests COMMAND OuroborosTests)

# Create CTest configuration
configure_file(
    "${CMAKE_CURRENT_SOURCE_DIR}/CTestCustom.cmake.in"
    "${CMAKE_BINARY_DIR}/CTestCustom.cmake"
    @ONLY
) 