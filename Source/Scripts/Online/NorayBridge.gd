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

var noray_address: String = "127.0.0.1"
var noray_port: int = 8890

func _ready() -> void:
	var noray: _Noray = get_node_or_null("/root/Noray")
	if noray == null:
		push_error("Noray Autoload not found!")
		return
		
	noray.connect("on_connect_nat", Callable(self, "_handle_connect_nat"))
	noray.connect("on_connect_relay", Callable(self, "_handle_connect_relay"))

func _setup_noray() -> Error:
	var noray: _Noray = get_node("/root/Noray")
	var err = await noray.connect_to_host(noray_address, noray_port)
	if err != OK: return err
		
	noray.register_host()
	await noray.on_pid
	
	err = await noray.register_remote()
	return err

func start_host(players: int) -> void:
	is_host = true
	max_players = players
	var noray: _Noray = get_node("/root/Noray")
	
	var err: Error = await _setup_noray()
	if err != OK:
		host_failed.emit("Failed to setup Noray connection.")
		return
		
	peer = ENetMultiplayerPeer.new()
	err = peer.create_server(noray.local_port, max_players)
	if err != OK:
		host_failed.emit("Cannot create ENet server")
		return
	
	peer.host.compress(ENetConnection.COMPRESS_RANGE_CODER)
	peer.host.channel_limit(20)
	
	multiplayer.multiplayer_peer = peer
	
	var sm: SceneMultiplayer = multiplayer as SceneMultiplayer
	if sm: sm.server_relay = true 
	
	if str(noray.oid) != "" and str(noray.oid) != "<null>":
		host_ready.emit(str(noray.oid))
	else:
		await noray.on_oid
		host_ready.emit(str(noray.oid))

func start_client(oid: String) -> void:
	is_host = false
	current_target_oid = oid
	var noray: _Noray = get_node("/root/Noray")
	
	var err = await _setup_noray()
	if err != OK:
		client_failed.emit("Failed to setup Noray client connection.")
		return
		
	noray.connect_nat(oid)

func _handle_connect_nat(address: String, port: int) -> void:
	var err = await _handle_connect(address, port)
	if err != OK and not is_host:
		print("NAT connect failed, retrying with Relay fallback...")
		var noray = get_node("/root/Noray")
		noray.connect_relay(current_target_oid)

func _handle_connect_relay(address: String, port: int):
	var err = await _handle_connect(address, port)
	if err != OK and not is_host:
		client_failed.emit("Relay connection also failed.")

func _handle_connect(address: String, port: int) -> Error:
	var handshake: _PacketHandshake = get_node("/root/PacketHandshake")
	var noray: _Noray = get_node("/root/Noray")
	
	if noray.local_port <= 0: 
		return ERR_UNCONFIGURED
	
	if is_host:
		var current_peer: ENetMultiplayerPeer = multiplayer.multiplayer_peer as ENetMultiplayerPeer
		return await handshake.over_enet_peer(current_peer, address, port)
	else:
		var udp: PacketPeerUDP = PacketPeerUDP.new()
		udp.bind(noray.local_port)
		udp.set_dest_address(address, port)
		
		var err = await handshake.over_packet_peer(udp)
		udp.close()
		
		if err != OK and err != ERR_BUSY:
			return err
			
		peer = ENetMultiplayerPeer.new()
		var client_err: Error = peer.create_client(address, port, 0, 0, 0, noray.local_port)
		if client_err != OK: return client_err
			
		peer.host.compress(ENetConnection.COMPRESS_RANGE_CODER)
		peer.host.channel_limit(20)
		multiplayer.multiplayer_peer = peer
		
		# Waits for you to connect
		while peer.get_connection_status() == MultiplayerPeer.CONNECTION_CONNECTING:
			await get_tree().process_frame
		
		# Either connected or failed
		if peer.get_connection_status() == MultiplayerPeer.CONNECTION_CONNECTED:
			client_connected.emit()
			return OK
		else:
			multiplayer.multiplayer_peer = null
			return ERR_CANT_CONNECT
