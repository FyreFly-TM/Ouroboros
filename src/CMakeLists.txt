# Ouroboros Core Library

# Collect all source files
file(GLOB_RECURSE OUROBOROS_SOURCES
    "*.cs"
)

# Exclude platform-specific files based on OS
if(NOT WIN32)
    list(FILTER OUROBOROS_SOURCES EXCLUDE REGEX ".*Windows.*\\.cs$")
endif()

if(NOT UNIX)
    list(FILTER OUROBOROS_SOURCES EXCLUDE REGEX ".*Linux.*\\.cs$")
endif()

if(NOT APPLE)
    list(FILTER OUROBOROS_SOURCES EXCLUDE REGEX ".*MacOS.*\\.cs$")
endif()

# Create the core library
add_library(OuroborosCore SHARED ${OUROBOROS_SOURCES})

# Set properties
set_property(TARGET OuroborosCore PROPERTY VS_DOTNET_TARGET_FRAMEWORK_VERSION "v6.0")
set_property(TARGET OuroborosCore PROPERTY VS_GLOBAL_LangVersion "latest")

# Add compile definitions
target_compile_definitions(OuroborosCore PRIVATE
    $<$<BOOL:${ENABLE_JIT}>:ENABLE_JIT>
    $<$<BOOL:${ENABLE_GPU}>:ENABLE_GPU>
)

# Create the main executable
add_executable(ouroboros Program.cs)
target_link_libraries(ouroboros OuroborosCore)

# Set executable properties
set_property(TARGET ouroboros PROPERTY VS_DOTNET_TARGET_FRAMEWORK_VERSION "v6.0")
set_property(TARGET ouroboros PROPERTY VS_GLOBAL_LangVersion "latest")

# Installation
install(TARGETS ouroboros DESTINATION bin)
install(TARGETS OuroborosCore DESTINATION lib) 