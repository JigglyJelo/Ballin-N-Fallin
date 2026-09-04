# ==========================================
# custom.py for compiling an optimized
# Godot 4.5.2 export template with scons
# Run: scons profile=custom.py
# ==========================================

# ==========================================
# Core Build & Optimization
# ==========================================
target = "template_release"
production = "yes"
optimize = "size"
lto = "full"
deprecated = "yes" # Idk why but the game doesn't work if disabled
debug_symbols = "no"
tools = "no"
threads = "yes"
disable_advanced_gui = "no"

# ==========================================
# Scripting Languages
# ==========================================
module_mono_enabled = "yes"
module_gdscript_enabled = "yes"

# ==========================================
# Rendering & Graphics APIs
# ==========================================
opengl3 = "yes"
vulkan = "no"
d3d12 = "no"
module_glslang_enabled = "no"

# ==========================================
# 3D, Physics, Spatial, & Navigation
# ==========================================
disable_3d = "yes"
disable_physics_3d = "yes"
disable_navigation_3d = "yes"
disable_xr = "yes"
module_godot_physics_3d_enabled = "no"
module_jolt_physics_enabled = "no"
module_navigation_3d_enabled = "no"
module_lightmapper_rd_enabled = "no"
module_navigation_enabled = "no"
module_camera_enabled = "no"
module_gridmap_enabled = "no"
module_meshoptimizer_enabled = "no"
module_vhacd_enabled = "no"
module_csg_enabled = "no"
module_bullet_enabled = "no"
module_recast_enabled = "no"
module_xatlas_unwrap_enabled = "no"
module_opensimplex_enabled = "no"

# ==========================================
# 3D Model Importers
# ==========================================
module_gltf_enabled = "no"
module_assimp_enabled = "no"
module_fbx_enabled = "no"
module_basis_universal_enabled = "no"

# ==========================================
# AR / VR / XR
# ==========================================
module_webxr_enabled = "no"
module_mobile_vr_enabled = "no"
module_openxr_enabled = "no"
openxr = "no"
module_arkit_enabled = "no"

# ==========================================
# Networking & Web
# ==========================================
module_multiplayer_enabled = "yes"
module_enet_enabled = "yes"
module_jsonrpc_enabled = "no"
module_upnp_enabled = "no"
module_mbedtls_enabled = "no"
module_webrtc_enabled = "no"
module_websocket_enabled = "no"

# ==========================================
# Audio & Video Media
# ==========================================
module_ogg_enabled = "yes"
module_minimp3_enabled = "yes"
minimp3_extra_formats = "no"
module_theora_enabled = "no"
module_webm_enabled = "no"
module_opus_enabled = "no"
module_stb_vorbis_enabled = "no"
module_interactive_music_enabled = "no"

# ==========================================
# Fonts & Text Server
# ==========================================
module_text_server_fb_enabled = "yes"
module_text_server_adv_enabled = "no"
module_msdfgen_enabled = "no"
graphite = "no"

# ==========================================
# Image & Texture Formats
# ==========================================
module_webp_enabled = "yes"
module_svg_enabled = "no"
module_bmp_enabled = "no"
module_ktx_enabled = "no"
module_tga_enabled = "no"
module_dds_enabled = "no"
module_jpg_enabled = "no"
module_squish_enabled = "no"
module_png_enabled = "no"
module_pvr_enabled = "no"
module_etc_enabled = "no"
module_etcpak_enabled = "no"
module_tinyexr_enabled = "no"
module_cvtt_enabled = "no"
module_astcenc_enabled = "no"
module_hdr_enabled = "no"
module_bcdec_enabled = "no"
module_betsy_enabled = "no"

# ==========================================
# Utilities & Compression
# ==========================================
brotli = "yes"
minizip = "no"
module_noise_enabled = "no"
module_regex_enabled = "no"
module_zip_enabled = "no"