// 2014.04
// Tracy Ma
// Original c++ version author: Glenn Fiedler <gaffer@gaffer.org>
using UnityEngine;
using System.Collections.Generic;

public class Address
{
    //	public bool Equals(Address other)
    //	{
    //		if()
    //	}
    public Address()
    {
        _address = 0;
        _port = 0;
    }
    public Address(char a, char b, char c, char d, ushort port)
    {
        _address = (uint)(a << 24);
        _address |= (uint)(b << 16);
        _address |= (uint)(c << 8);
        _address |= (uint)(d);
        _port = port;
    }
    public Address(uint address, ushort port)
    {
        _address = address;
        _port = port;
    }

    public uint GetAddress()
    {
        return _address;
    }

    public ushort GetPort()
    {
        return _port;
    }
    public override bool Equals(System.Object obj)
    {
        if (obj == null)
        {
            return false;
        }
        Address a = obj as Address;
        if ((System.Object)a == null)
        {
            return false;
        }
        return (_address == a._address) && (_port == a._port);
    }
    public override int GetHashCode()
    {
        return (int)(_address ^ _port);
    }
    public static bool operator ==(Address one, Address other)
    {
        return one._address == other._address && one._port == other._port;
    }
    public static bool operator !=(Address one, Address other)
    {
        return !(one == other);
    }
    public static bool operator <(Address one, Address other)
    {
        if (one._address < other._address)
            return true;
        if (one._address > other._address)
            return false;
        else
            return one._port < other._port;
    }
    public static bool operator >(Address one, Address other)
    {
        if (one._address > other._address)
            return true;
        if (one._address < other._address)
            return false;
        else
            return one._port > other._port;
    }
    public void Reset()
    {
        _address = 0;
        _port = 0;
    }
    uint _address;
    ushort _port;
}

public class Socket
{
    public Socket()
    {
        socket = null;
    }
    ~Socket()
    {
        Close();
    }
    /// <summary>
    /// 初始化套接字和sendorRemote
    /// </summary>
    /// <param name="port">端口</param>
    /// <returns></returns>
    public bool Open(ushort port)
    {
        socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork,
                                                        System.Net.Sockets.SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
        socket.Blocking = false;
        return true;
    }
    public void Close()
    {
        socket.Close();
    }
    //public bool IsOpen()
    //{
    //    return socket.Available();
    //}
    public bool Send(ref System.Net.IPEndPoint ipep, byte[] data, int size)
    {
        if (socket == null)
        {
            return false;
        }

        int count = socket.SendTo(data, size, System.Net.Sockets.SocketFlags.None, ipep);
        if (count > 0)
            return true;
        else
            return false;
    }
    public int Receive(ref System.Net.EndPoint sendor, byte[] data, int size)
    {
        int count = socket.ReceiveFrom(data, ref sendor);
        return count;
    }

    System.Net.Sockets.Socket socket = null;
}

public class Connection
{
    public enum Mode
    {
        None,
        Server,
        Client
    };
    Mode mode;
    uint protocolID;
    float timeout;
    bool running;
    enum State
    {
        Disconnected,
        Listening,
        Connecting,
        ConnectFail,
        Connected
    };
    State state;
    float timeoutAccumulator;
    Socket socket = new Socket();
    System.Net.IPEndPoint address = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 1912);

    void ClearData()
    {
        state = State.Disconnected;
        timeoutAccumulator = 0.0f;
        //address.Reset();
    }

    protected virtual void OnStart() { }
    protected virtual void OnStop() { }
    protected virtual void OnConnect() { }
    protected virtual void OnDisconnect() { }
    // constructor
    //public Connection()
    //{ }
    public Connection(uint pID, float TO)
    {
        this.protocolID = pID;
        this.timeout = TO;
        mode = Mode.None;
        running = false;
        ClearData();
    }

    public bool Start(ushort port)
    {
        Debug.Log("start connection on port " + port);
        if (!socket.Open(port))
        {
            return false;
        }
        running = true;
        OnStart();
        return true;
    }

    public void Stop()
    {
        Debug.Log("stop connection");
        bool connected = IsConnected();
        ClearData();
        socket.Close();
        running = false;
        if (connected)
            OnDisconnect();
        OnStop();
    }
    public bool IsRunning()
    {
        return running;
    }
    public void Listen()
    {
        Debug.Log("server listening for connection");
        bool connected = IsConnected();
        ClearData();
        if (connected)
            OnDisconnect();
        mode = Mode.Server;
        state = State.Listening;
    }
    public void Connect(ref System.Net.IPEndPoint addr)
    {
        //print("client connecting to %hhu.%hhu.%hhu.%hhu:%d\n",
        //        addr.GetA(), addr.GetB(), addr.GetC(), addr.GetD(), addr.GetPort());
        bool connected = IsConnected();
        ClearData();
        if (connected)
            OnDisconnect();
        mode = Mode.Client;
        state = State.Connecting;
        address = addr;
        Debug.Log("(0)address = " + address.ToString());
    }

    public bool IsConnecting()
    {
        return state == State.Connecting;
    }

    public bool ConnectFailed()
    {
        return state == State.ConnectFail;
    }

    public bool IsConnected()
    {
        return state == State.Connected;
    }

    public bool IsListening()
    {
        return state == State.Listening;
    }
    public Mode GetMode()
    {
        return mode;
    }
    public virtual void Update(float deltaTime)
    {
        timeoutAccumulator += deltaTime;
        if (timeoutAccumulator > timeout)
        {
            if (state == State.Connecting)
            {
                Debug.Log("connect timed out");
                ClearData();
                state = State.ConnectFail;
                OnDisconnect();
            }
            else if (state == State.Connected)
            {
                Debug.Log("connnection timed out");
                ClearData();
                if (state == State.Connecting)
                {
                    state = State.ConnectFail;
                }
                OnDisconnect();
            }
        }
    }
    public static string ByteArrayToString(byte[] ba)
    {
        string hex = System.BitConverter.ToString(ba);
        return hex.Replace("-", "");
    }
    public virtual bool SendPacket(byte[] data, int size)
    {
        //if (address.GetAddress() == 0)
        //{
        //    return false;
        //}
        byte[] packet = new byte[size + 4];
        packet[0] = (byte)(protocolID >> 24);
        packet[1] = (byte)((protocolID >> 16) & 0xff);
        packet[2] = (byte)((protocolID >> 8) & 0xff);
        packet[3] = (byte)(protocolID & 0xff);
        System.Buffer.BlockCopy(data, 0, packet, 4, data.Length);
        Debug.Log(ByteArrayToString(packet));

        Debug.Log("IP = " + address.ToString());
        return socket.Send(ref address, packet, size + 4);

    }
    public virtual int ReceivePacket(ref byte[] data, int size)
    {
        byte[] packet = new byte[size + 4];
        System.Net.IPEndPoint remoteIpEp = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0);
        System.Net.EndPoint remoteEp = (System.Net.EndPoint)remoteIpEp;
        int count = socket.Receive(ref remoteEp, packet, size + 4);
        if (count == 0)
        {
            return 0;
        }
        if (count <= 4)
        {
            return 0;
        }
        if (packet[0] != (byte)(protocolID >> 24) ||
                packet[1] != (byte)((protocolID >> 16) & 0xFF) ||
                packet[2] != (byte)((protocolID >> 8) & 0xFF) ||
                packet[3] != (byte)(protocolID & 0xFF))
        {
            return 0;
        }
        if (mode == Mode.Server && !IsConnected())
        {
            Debug.Log("server accepts connection from client ");
            state = State.Connected;
            address = remoteIpEp;
            OnConnect();
        }
        // TODO
        if (address.Equals(remoteIpEp))
        {
            if (state == State.Connecting)
            {
                state = State.Connected;
                OnConnect();
            }
            timeoutAccumulator = 0.0f;
            System.Buffer.BlockCopy(packet, 4, data, 0, count - 4);
            return count - 4;
        }
        return 0;
    }
    public virtual int GetHeaderSize()
    {
        return 4;
    }
}
public struct PacketData
{
    public uint sequence;
    public int size;
    public float time;
}

public class PacketQueue : LinkedList<PacketData>
{
    bool sequence_more_recent(uint s1, uint s2, uint max_sequence)
    {
        return ((s1 > s2) && (s1 - s2 <= max_sequence / 2)) || ((s2 > s1) && (s2 - s1 > max_sequence / 2));
    }

    public bool exists(uint sequence)
    {

        foreach (PacketData pd in this)
        {
            if (pd.sequence == sequence)
            {
                return true;
            }
        }
        return false;
    }
    public void insert_sorted(PacketData p, uint max_sequence)
    {
        if (this.Count == 0)
        {
            this.AddLast(p);
        }
        else
        {
            if (!sequence_more_recent(p.sequence, this.First.Value.sequence, max_sequence))
            {
                this.AddFirst(p);
            }
            else if (sequence_more_recent(p.sequence, this.Last.Value.sequence, max_sequence))
            {
                this.AddLast(p);
            }
            else
            {
                for (LinkedListNode<PacketData> node = this.First; node != this.Last.Next; node = node.Next)
                {
                    if (sequence_more_recent(node.Value.sequence, p.sequence, max_sequence))
                    {
                        this.AddAfter(node, p);
                        break;
                    }
                }
            }
        }
    }
    public void verify_sorted(uint max_sequence)
    {
        if (this.Count < 2)
        {
            Debug.LogError("queue is too short");
        }
        for (LinkedListNode<PacketData> node = this.First; node != this.Last.Next; node = node.Next)
        {
            if (node.Next != null)
            {
                if (sequence_more_recent(node.Value.sequence, node.Next.Value.sequence, max_sequence))
                {
                    Debug.LogError("verify_sorted fail!");
                }
            }

        }
    }

}
public class ReliabilitySystem
{
    public ReliabilitySystem(uint max_sequence = 0xffffffff)
    {
        this.max_sequence = max_sequence;
        Reset();
    }
    public void Reset()
    {
        local_sequence = 0;
        remote_sequence = 0;
        sentQueue.Clear();
        receivedQueue.Clear();
        pendingAckQueue.Clear();
        ackedQueue.Clear();
        sent_packets = 0;
        recv_packets = 0;
        lost_packets = 0;
        acked_packets = 0;
        sent_bandwidth = 0.0f;
        acked_bandwidth = 0.0f;
        rtt = 0.0f;
        rtt_maximum = 0.0f;
    }
    public void PacketSent(int size)
    {
        if (sentQueue.exists(local_sequence))
        {
            Debug.Log("local sequence exists " + local_sequence);
        }
        PacketData data;
        data.sequence = local_sequence;
        data.size = size;
        data.time = 0.0f;
        sentQueue.AddLast(data);
        pendingAckQueue.AddLast(data);
        sent_packets++;
        local_sequence++;
        if (local_sequence > max_sequence)
        {
            local_sequence = 0;
        }
    }
    static bool sequence_more_recent(uint s1, uint s2, uint max_sequence)
    {
        return ((s1 > s2) && (s1 - s2 <= max_sequence / 2)) || ((s2 > s1) && (s2 - s1 > max_sequence / 2));
    }
    public void PacketReceived(uint sequence, int size)
    {
        recv_packets++;
        if (receivedQueue.exists(sequence))
            return;
        PacketData data;
        data.sequence = sequence;
        data.size = size;
        data.time = 0.0f;
        receivedQueue.AddLast(data);
        if (sequence_more_recent(sequence, remote_sequence, max_sequence))
        {
            remote_sequence = sequence;
        }
    }
    public uint GenerateAckBits()
    {
        return generate_ack_bits(GetRemoteSequence(), ref receivedQueue, max_sequence);
    }
    public void ProcessAck(uint ack, uint ack_bits)
    {
        process_ack(ack, ack_bits, ref pendingAckQueue, ref ackedQueue, ref acks, acked_packets, rtt, max_sequence);
    }
    public void Update(float deltaTime)
    {
        acks.Clear();
        AdvanceQueueTime(deltaTime);
        UpdateQueues();
        UpdateStatus();
    }
    public static uint bit_index_for_sequence(uint sequence, uint ack, uint max_sequence)
    {
        if (sequence > ack)
        {
            return ack + (max_sequence - sequence);
        }
        else
        {
            return ack - 1 - sequence;
        }
    }
    public static uint generate_ack_bits(uint ack, ref PacketQueue received_queue, uint max_sequence)
    {
        uint ack_bits = 0;
        foreach (PacketData received in received_queue)
        {
            if (received.sequence == ack || sequence_more_recent(received.sequence, ack, max_sequence))
                break;
            uint bit_index = bit_index_for_sequence(received.sequence, ack, max_sequence);
            if (bit_index <= 31)
            {
                ack_bits |= 1U << (int)bit_index;
            }
        }
        return ack_bits;
    }
    public static void process_ack(uint ack, uint ack_bits, ref PacketQueue pending_ack_queue,
                                    ref PacketQueue acked_queue, ref List<uint> acks,
                                    uint acked_packets, float rtt, uint max_sequence)
    {
        if (pending_ack_queue.Count == 0)
        {
            return;
        }
        var node = pending_ack_queue.First;
        while (node != null)
        {
            var nextnode = node.Next;

            bool acked = false;
            if (node.Value.sequence == ack)
            {
                acked = true;
            }
            else if (!sequence_more_recent(node.Value.sequence, ack, max_sequence))
            {
                uint bit_index = bit_index_for_sequence(node.Value.sequence, ack, max_sequence);
                if (bit_index <= 31)
                {
                    acked = System.Convert.ToBoolean((ack_bits >> (int)bit_index) & 1);
                }
            }
            if (acked)
            {
                rtt += (node.Value.time - rtt) * 0.1f;

                acked_queue.insert_sorted(node.Value, max_sequence);
                acks.Add(node.Value.sequence);
                acked_packets++;
                pending_ack_queue.Remove(node);
                node = nextnode;
            }
            else
            {
                node = nextnode;
            }
        }

    }
    public uint GetLocalSequence()
    {
        return local_sequence;
    }
    public uint GetRemoteSequence()
    {
        return remote_sequence;
    }
    public uint GetMaxSequence()
    {
        return max_sequence;
    }
    public void GetAcks()
    {

    }
    public uint GetSentPackets()
    {
        return sent_packets;
    }
    public uint GetReceivePackets()
    {
        return recv_packets;
    }
    public uint GetLostPackets()
    {
        return lost_packets;
    }
    public uint GetAckedPackets()
    {
        return acked_packets;
    }
    public float GetSentBandwidth()
    {
        return sent_bandwidth;
    }
    public float GetAckedBandwidth()
    {
        return acked_bandwidth;
    }

    public float GetRoundTripTime()
    {
        return rtt;
    }
    public int GetHeaderSize()
    {
        return 12;
    }
    protected void AdvanceQueueTime(float deltaTime)
    {
        PacketData data;
        for (LinkedListNode<PacketData> node = sentQueue.First; node != sentQueue.Last.Next; node = node.Next)
        {
            data.sequence = node.Value.sequence;
            data.size = node.Value.size;
            data.time = node.Value.time + deltaTime;
            node.Value = data;
        }
        for (LinkedListNode<PacketData> node = receivedQueue.First; node != receivedQueue.Last.Next; node = node.Next)
        {
            data.sequence = node.Value.sequence;
            data.size = node.Value.size;
            data.time = node.Value.time + deltaTime;
            node.Value = data;
        }
        for (LinkedListNode<PacketData> node = pendingAckQueue.First; node != pendingAckQueue.Last.Next; node = node.Next)
        {
            data.sequence = node.Value.sequence;
            data.size = node.Value.size;
            data.time = node.Value.time + deltaTime;
            node.Value = data;
        }
        for (LinkedListNode<PacketData> node = ackedQueue.First; node != ackedQueue.Last.Next; node = node.Next)
        {
            data.sequence = node.Value.sequence;
            data.size = node.Value.size;
            data.time = node.Value.time + deltaTime;
            node.Value = data;
        }
    }
    protected void UpdateQueues()
    {
        const float epsilon = 0.001f;
        while ((sentQueue.Count > 0) && (sentQueue.First.Value.time > (rtt_maximum + epsilon)))
        {
            sentQueue.RemoveFirst();
        }
        if (receivedQueue.Count > 0)
        {
            uint latest_sequence = receivedQueue.Last.Value.sequence;
            uint minimum_sequence = latest_sequence >= 34 ? (latest_sequence - 34) : max_sequence - (34 - latest_sequence);
            while ((receivedQueue.Count > 0) && !sequence_more_recent(receivedQueue.First.Value.sequence, minimum_sequence, max_sequence))
            {
                receivedQueue.RemoveFirst();
            }
        }
        while (ackedQueue.Count > 0 && ackedQueue.First.Value.time > rtt_maximum * 2 - epsilon)
        {
            ackedQueue.RemoveFirst();
        }
        while (pendingAckQueue.Count > 0 && pendingAckQueue.First.Value.time > rtt_maximum + epsilon)
        {
            pendingAckQueue.RemoveFirst();
            lost_packets++;
        }
    }
    protected void UpdateStatus()
    {
        int sent_bytes_per_second = 0;
        foreach (PacketData data in sentQueue)
        {
            sent_bytes_per_second += data.size;
        }
        int acked_packets_per_second = 0;
        int acked_bytes_per_second = 0;
        foreach (PacketData data in ackedQueue)
        {
            if (data.time >= rtt_maximum)
            {
                acked_packets_per_second++;
                acked_bytes_per_second += data.size;
            }
        }
        sent_bytes_per_second = (int)(sent_bytes_per_second / rtt_maximum);
        acked_bytes_per_second = (int)(acked_bytes_per_second / rtt_maximum);
        sent_bandwidth = sent_bytes_per_second * (8 / 1000.0f);
        acked_bandwidth = acked_bytes_per_second * (8 / 1000.0f);
    }
    uint max_sequence;
    uint local_sequence;
    uint remote_sequence;

    uint sent_packets;
    uint recv_packets;
    uint lost_packets;
    uint acked_packets;

    float sent_bandwidth;
    float acked_bandwidth;
    float rtt;
    float rtt_maximum;

    List<uint> acks;

    PacketQueue sentQueue = new PacketQueue();
    PacketQueue pendingAckQueue = new PacketQueue();
    PacketQueue receivedQueue = new PacketQueue();
    PacketQueue ackedQueue = new PacketQueue();
}

public class ReliableConnection : Connection
{
    public ReliableConnection(uint protocolId, float timeout, uint max_sequence = 0xffffffff)
        : base(protocolId, timeout)
    {
        reliabilitySystem = new ReliabilitySystem(max_sequence);
        ClearData();
    }
    ~ReliableConnection()
    {
        if (base.IsRunning())
        {
            base.Stop();
        }
    }
    bool SendPacket(ref byte[] data, int size)
    {
        const int header = 12;
        byte[] packet = new byte[header + size];
        uint seq = reliabilitySystem.GetLocalSequence();
        uint ack = reliabilitySystem.GetRemoteSequence();
        uint ack_bits = reliabilitySystem.GenerateAckBits();
        WriteHeader(ref packet, seq, ack, ack_bits);
        System.Buffer.BlockCopy(data, 0, packet, header, data.Length);
        if (!base.SendPacket(packet, size + header))
        {
            return false;
        }
        reliabilitySystem.PacketSent(size);
        return true;
    }
    public override int ReceivePacket(ref byte[] data, int size)
    {
        const int header = 12;
        if (size <= header)
        {
            return 0;
        }
        byte[] packet = new byte[header + size];
        int count = base.ReceivePacket(ref packet, size + header);
        if (count == 0)
            return 0;
        if (count <= header)
            return 0;
        uint packet_sequence = 0;
        uint packet_ack = 0;
        uint packet_ack_bits = 0;
        ReadHeader(ref packet, ref packet_sequence, ref packet_ack, ref packet_ack_bits);
        reliabilitySystem.PacketReceived(packet_sequence, count - header);
        reliabilitySystem.ProcessAck(packet_ack, packet_ack_bits);
        System.Buffer.BlockCopy(packet, header, data, 0, count - header);
        return count - header;
    }
    public void WriteInteger(ref byte[] data, uint value, int offset)
    {
        data[offset] = (byte)(value >> 24);
        data[offset + 1] = (byte)((value >> 16) & 0xff);
        data[offset + 2] = (byte)((value >> 8) & 0xff);
        data[offset + 3] = (byte)(value & 0xff);
    }
    public void WriteHeader(ref byte[] header, uint sequence, uint ack, uint ack_bits)
    {
        WriteInteger(ref header, sequence, 0);
        WriteInteger(ref header, ack, 4);
        WriteInteger(ref header, ack_bits, 8);
    }

    public void ReadInteger(ref byte[] data, ref uint value, int offset)
    {
        value = ((uint)(data[offset] << 24) |
                 (uint)(data[offset + 1] << 16) |
                 (uint)(data[offset + 2] << 8) |
                 (uint)(data[offset + 3]));
    }
    public void ReadHeader(ref byte[] header, ref uint sequence, ref uint ack, ref uint ack_bits)
    {
        ReadInteger(ref header, ref sequence, 0);
        ReadInteger(ref header, ref ack, 4);
        ReadInteger(ref header, ref ack_bits, 8);
    }
    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        reliabilitySystem.Update(deltaTime);
    }
    public override int GetHeaderSize()
    {
        return base.GetHeaderSize() + reliabilitySystem.GetHeaderSize();
    }
    public ReliabilitySystem GetReliableSystem()
    {
        return reliabilitySystem;
    }
    protected override void OnStop()
    {
        ClearData();
    }

    protected override void OnDisconnect()
    {
        ClearData();
    }
    void ClearData()
    {
        reliabilitySystem.Reset();
    }
    ReliabilitySystem reliabilitySystem;
}