extends Node
class_name NorayBridge

signal host_ready(oid: String)
signal host_failed(err_msg: String)
signal client_connected()
signal client_failed(err_msg: String)

var peer: ENetMultiplayerPeer
var is_host: bool = false
var max_players: int = 4
var current_target_oid: String = ""

# Change to your remote server IP when going into production
var noray_address: String = "foxssake.studio" 
var noray_port: int = 8890

func _ready():
	var noray = get_node_or_null("/root/Noray")
	if noray == null:
		push_error("Noray Autoload not found! Make sure netfox.noray is enabled in Project Settings.")
		return
		
	noray.connect("on_connect_nat", Callable(self, "_handle_connect_nat"))
	noray.connect("on_connect_relay", Callable(self, "_handle_connect_relay"))

# BOTH Hosts and Clients must run this to get a valid local_port from the NAT
func _setup_noray() -> Error:
	var noray = get_node("/root/Noray")
	var err = await noray.connect_to_host(noray_address, noray_port)
	if err != OK:
		return err
		
	noray.register_host()
	await noray.on_pid
	
	err = await noray.register_remote()
	if err != OK:
		return err
		
	return OK

func start_host(players: int):
	is_host = true
	max_players = players
	var noray = get_node("/root/Noray")
	
	var err = await _setup_noray()
	if err != OK:
		emit_signal("host_failed", "Failed to setup Noray connection.")
		return
		
	peer = ENetMultiplayerPeer.new()
	err = peer.create_server(noray.local_port, max_players)
	if err != OK:
		emit_signal("host_failed", "Cannot create ENet server")
		return
	
	peer.host.compress(ENetConnection.COMPRESS_RANGE_CODER)
	peer.host.channel_limit(20) 
	
	multiplayer.multiplayer_peer = peer
	multiplayer.server_relay = true # Required for P2P client-to-client routing
	
	if str(noray.oid) != "" and str(noray.oid) != "<null>":
		emit_signal("host_ready", str(noray.oid))
	else:
		await noray.on_oid
		emit_signal("host_ready", str(noray.oid))

func start_client(oid: String):
	is_host = false
	current_target_oid = oid
	var noray = get_node("/root/Noray")
	
	var err = await _setup_noray()
	if err != OK:
		emit_signal("client_failed", "Failed to setup Noray client connection.")
		return
		
	noray.connect_nat(oid)

func _handle_connect_nat(address: String, port: int):
	var err = await _handle_connect(address, port)
	
	# If client failed to connect over NAT direct punchthrough, try fallback relay
	if err != OK and not is_host:
		print("NAT connect failed, retrying with Relay fallback...")
		var noray = get_node("/root/Noray")
		noray.connect_relay(current_target_oid)

func _handle_connect_relay(address: String, port: int):
	var err = await _handle_connect(address, port)
	if err != OK and not is_host:
		emit_signal("client_failed", "Relay connection also failed.")

func _handle_connect(address: String, port: int) -> Error:
	var handshake = get_node("/root/PacketHandshake")
	var noray = get_node("/root/Noray")
	
	if not noray.local_port:
		return ERR_UNCONFIGURED
	
	if is_host:
		# Official API requires over_enet_peer
		var current_peer = multiplayer.multiplayer_peer as ENetMultiplayerPeer
		var err = await handshake.over_enet_peer(current_peer, address, port)
		return err
	else:
		var udp = PacketPeerUDP.new()
		udp.bind(noray.local_port)
		udp.set_dest_address(address, port)
		
		var err = await handshake.over_packet_peer(udp)
		udp.close()
		
		# ERR_BUSY means partial success, we can still proceed
		if err != OK and err != ERR_BUSY:
			return err
			
		peer = ENetMultiplayerPeer.new()
		var client_err = peer.create_client(address, port, 0, 0, 0, noray.local_port)
		if client_err != OK:
			return client_err
			
		peer.host.compress(ENetConnection.COMPRESS_RANGE_CODER)
		peer.host.channel_limit(20)
		multiplayer.multiplayer_peer = peer
		
		# Async wait for the connection to finalize before emitting success
		while peer.get_connection_status() == MultiplayerPeer.CONNECTION_CONNECTING:
			await get_tree().process_frame
			
		if peer.get_connection_status() == MultiplayerPeer.CONNECTION_CONNECTED:
			emit_signal("client_connected")
			return OK
		else:
			multiplayer.multiplayer_peer = null
			return ERR_CANT_CONNECT
