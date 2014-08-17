using UnityEngine;
using System.Collections.Generic;

public class unittest : MonoBehaviour
{
    void test_ipendpoint_endpoint_equal()
    {
        System.Net.IPEndPoint ipep = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 1912);
        System.Net.EndPoint ep = (System.Net.IPEndPoint)ipep;
        print(ipep.ToString());
        print(ep.ToString());
        print(ipep.Equals(ep));

        //System.Net.IPEndPoint ipep2 = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0);
        //System.Net.EndPoint sender = (System.Net.EndPoint)ipep;
        //sender = new 
    }
    void test_remove_items_in_loops()
    {
        var myList = new List<int> { 0, 1, 2, 3, 4, 5 };
        print("before");
        foreach (int i in myList)
        {
            print(i);
        }
        for (int i = 0; i < myList.Count - 1; i++)
        {
            if (myList[i] >= 3)
            {
                myList.RemoveAt(i);
            }
        }
        print("after");
        foreach (int i in myList)
        {
            print(i);
        }
    }
    void test_list_pushback()
    {
        var myList = new List<int> { 0, 1, 2, 3, 4, 5 };
        myList.Add(11);
        print(myList[myList.Count - 1]);
    }
    void test_remove_in_while()
    {
        var myList = new List<int> { 0, 1, 2, 3, 4, 5 };
        while (myList.Count > 0 && myList[0] < 5)
        {
            myList.RemoveAt(0);
            print(myList.Count);
        }
    }
    void test_packet_queue()
    {
        uint MaximumSequence = 255;
        PacketQueue packetQueue = new PacketQueue();
        PacketData data;

        print("check insert back");
        for (uint i = 0; i < 100; ++i)
        {
            data = new PacketData();
            data.sequence = i;
            packetQueue.insert_sorted(data, MaximumSequence);
            if (i > 1)
                packetQueue.verify_sorted(MaximumSequence);
        }

        print("check insert front");
        packetQueue.Clear();
        for (uint i = 100; i < 0; ++i)
        {
            data = new PacketData();
            data.sequence = i;
            packetQueue.insert_sorted(data, MaximumSequence);
            if (i > 1)
                packetQueue.verify_sorted(MaximumSequence);
        }
        print("check insert random");
        packetQueue.Clear();
        for (uint i = 100; i < 0; ++i)
        {
            data = new PacketData();
            data.sequence = (uint)(UnityEngine.Random.value * 10) & 0xff;
            packetQueue.insert_sorted(data, MaximumSequence);
            if (i > 1)
                packetQueue.verify_sorted(MaximumSequence);
        }
        print("check insert wrap around");
        packetQueue.Clear();
        for (uint i = 200; i <= 255; ++i)
        {
            data = new PacketData();
            data.sequence = i;
            packetQueue.insert_sorted(data, MaximumSequence);
            if (i > 201)
                packetQueue.verify_sorted(MaximumSequence);
        }
        for (uint i = 0; i <= 50; ++i)
        {
            data = new PacketData();
            data.sequence = i;
            packetQueue.insert_sorted(data, MaximumSequence);
            if (i > 1)
                packetQueue.verify_sorted(MaximumSequence);
        }
    }
    void test_reliability_system()
    {
        const uint MaximumSequence = 255;
        Debug.Log(ReliabilitySystem.bit_index_for_sequence(255, 1, MaximumSequence));
        /////////////////////////////////
        print("check generate ack bits");
        PacketQueue packetQueue = new PacketQueue();
        for (uint i = 0; i < 32; ++i)
        {
            PacketData data = new PacketData();
            data.sequence = i;
            packetQueue.insert_sorted(data, MaximumSequence);

        }
        packetQueue.verify_sorted(MaximumSequence);
        if (ReliabilitySystem.generate_ack_bits(32, ref packetQueue, MaximumSequence) == 0xFFFFFFFF)
        {
            Debug.Log("OK");
        }
        if (ReliabilitySystem.generate_ack_bits(31, ref packetQueue, MaximumSequence) == 0x7FFFFFFF)
        {
            Debug.Log("OK");
        }
        if (ReliabilitySystem.generate_ack_bits(33, ref packetQueue, MaximumSequence) == 0xFFFFFFFE)
        {
            Debug.Log("OK");
        }
        if (ReliabilitySystem.generate_ack_bits(16, ref packetQueue, MaximumSequence) == 0x0000FFFF)
        {
            Debug.Log("OK");
        }
        if (ReliabilitySystem.generate_ack_bits(48, ref packetQueue, MaximumSequence) == 0xFFFF0000)
        {
            Debug.Log("OK");
        }

        /////////////////////////////////
        print("check generate ack bits with wrap");


        packetQueue.Clear();
        for (uint i = 255 - 31; i <= 255; ++i)
        {
            PacketData data = new PacketData();
            data.sequence = i;
            packetQueue.insert_sorted(data, MaximumSequence);

        }
        packetQueue.verify_sorted(MaximumSequence);
        if (packetQueue.Count == 32)
        {
            Debug.Log("ack wrap ok");
        };
        if (ReliabilitySystem.generate_ack_bits(0, ref packetQueue, MaximumSequence) == 0xFFFFFFFF)
        {
            Debug.Log("ack wrap ok");
        };
        if (ReliabilitySystem.generate_ack_bits(255, ref packetQueue, MaximumSequence) == 0x7FFFFFFF)
        {
            Debug.Log("ack wrap ok");
        };
        if (ReliabilitySystem.generate_ack_bits(1, ref packetQueue, MaximumSequence) == 0xFFFFFFFE)
        {
            Debug.Log("ack wrap ok");
        };
        if (ReliabilitySystem.generate_ack_bits(240, ref packetQueue, MaximumSequence) == 0x0000FFFF)
        {
            Debug.Log("ack wrap ok");
        };
        if (ReliabilitySystem.generate_ack_bits(16, ref packetQueue, MaximumSequence) == 0xFFFF0000)
        {
            Debug.Log("ack wrap ok");
        };

        print("check process ack (1)");
        PacketQueue pendingAckQueue = new PacketQueue();
        for (uint i = 0; i < 33; ++i)
        {
            PacketData data = new PacketData();
            data.sequence = i;
            data.time = 0.0f;
            pendingAckQueue.insert_sorted(data, MaximumSequence);

        }
        pendingAckQueue.verify_sorted(MaximumSequence);
        PacketQueue ackedQueue = new PacketQueue();
        List<uint> acks = new List<uint>();
        float rtt = 0.0f;
        uint acked_packets = 0;
        ReliabilitySystem.process_ack(32, 0xFFFFFFFF, ref  pendingAckQueue, ref ackedQueue, ref  acks, acked_packets, rtt, MaximumSequence);
        if (acks.Count == 33)
        {
            Debug.Log("process ack (1) ok");
        };
        if (acked_packets == 33)
        {
            Debug.Log("process ack (1) ok");
        };
        if (ackedQueue.Count == 33)
        {
            Debug.Log("process ack (1) ok");
        };
        if (pendingAckQueue.Count == 0)
        {
            Debug.Log("process ack (1) ok");
        };
        ackedQueue.verify_sorted(MaximumSequence);
        for (int i = 0; i < acks.Count; ++i)
        {
            Debug.Log(acks[i]);
        }

        for (LinkedListNode<PacketData> node = ackedQueue.First; node != ackedQueue.Last.Next; node = node.Next)
        {
            Debug.Log(node.Value.sequence);
        }

        ///////////////////////////////////////////////
        print("check process ack (2)");
        {
            PacketQueue pendingAckQueue2 = new PacketQueue();
            for (uint i = 0; i < 33; ++i)
            {
                PacketData data = new PacketData();
                data.sequence = i;
                data.time = 0.0f;
                pendingAckQueue2.insert_sorted(data, MaximumSequence);

            }
            pendingAckQueue2.verify_sorted(MaximumSequence);
            PacketQueue ackedQueue2 = new PacketQueue();
            List<uint> acks2 = new List<uint>();
            float rtt2 = 0.0f;
            uint acked_packets2 = 0;
            ReliabilitySystem.process_ack(32, 0x0000FFFF, ref pendingAckQueue2, ref ackedQueue2, ref acks2, acked_packets2, rtt2, MaximumSequence);
            if (acks2.Count == 17)
            {
                Debug.Log("process ack (2) ok");
            };
            if (acked_packets2 == 17)
            {
                Debug.Log("process ack (2) ok");
            };
            if (ackedQueue2.Count == 17)
            {
                Debug.Log("process ack (2) ok");
            };
            if (pendingAckQueue2.Count == 33 - 17)
            {
                Debug.Log("process ack (2) ok");
            };
            ackedQueue2.verify_sorted(MaximumSequence);
            for (LinkedListNode<PacketData> node = pendingAckQueue2.First; node != pendingAckQueue2.Last.Next; node = node.Next)
            {
                Debug.Log(node.Value.sequence);
            }
            print("===========================");
            for (LinkedListNode<PacketData> node = ackedQueue2.First; node != ackedQueue2.Last.Next; node = node.Next)
            {
                Debug.Log(node.Value.sequence);
            }
            for (int i = 0; i < acks2.Count; ++i)
            {
                if (acks2[i] == i + 16)
                {
                    Debug.Log("process ack (2) ok");
                };
            }

        }

        //////////////////////////////////////////////
        print("check process ack (3)\n");
        {
            PacketQueue pendingAckQueue3 = new PacketQueue();
            for (uint i = 0; i < 32; ++i)
            {
                PacketData data = new PacketData();
                data.sequence = i;
                data.time = 0.0f;
                pendingAckQueue3.insert_sorted(data, MaximumSequence);

            }
            pendingAckQueue3.verify_sorted(MaximumSequence);
            PacketQueue ackedQueue3 = new PacketQueue();
            List<uint> acks3 = new List<uint>();
            float rtt3 = 0.0f;
            uint acked_packets3 = 0;
            ReliabilitySystem.process_ack(48, 0xFFFF0000, ref pendingAckQueue3, ref ackedQueue3, ref acks3, acked_packets3, rtt3, MaximumSequence);
            if (acks3.Count == 16)
            {
                Debug.Log("process ack(3) ok");
            };
            if (acked_packets3 == 16)
            {
                Debug.Log("process ack(3) ok");
            };
            if (ackedQueue3.Count == 16)
            {
                Debug.Log("process ack(3) ok");
            };
            if (pendingAckQueue3.Count == 16)
            {
                Debug.Log("process ack(3) ok");
            };
            ackedQueue3.verify_sorted(MaximumSequence);

            for (LinkedListNode<PacketData> node = pendingAckQueue3.First; node != pendingAckQueue3.Last.Next; node = node.Next)
            {
                Debug.Log(node.Value.sequence);
            }
            print("===========================");
            for (LinkedListNode<PacketData> node = ackedQueue3.First; node != ackedQueue3.Last.Next; node = node.Next)
            {
                Debug.Log(node.Value.sequence);
            }
            for (int i = 0; i < acks3.Count; ++i)
            {
                if (acks3[i] == i + 16)
                {
                    Debug.Log("process ack (3) ok");
                };
            }
        }
    }

    void test_join()
    {
        print("-----------------------------------------------------");
        print("test join");
        print("-----------------------------------------------------");

        ushort ServerPort = 19120;
        const int ClientPort = 19121;
        const int ProtocolId = 0x11112222;
        const float DeltaTime = 0.001f;
        const float TimeOut = 1.0f;

        ReliableConnection client = new ReliableConnection(ProtocolId, TimeOut);
        ReliableConnection server = new ReliableConnection(ProtocolId, TimeOut);

        if (!client.Start(ClientPort))
        {
            Debug.LogError("");
        };
        if (!server.Start(ServerPort))
        {
            Debug.LogError("");
        };
        System.Net.IPEndPoint addr = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 1912);
        client.Connect(ref addr);
        server.Listen();

        while (true)
        {
            if (client.IsConnected() && server.IsConnected())
                break;

            if (!client.IsConnecting() && client.ConnectFailed())
                break;

            byte[] client_packet = { 0x00, 0x10, 0x20, 0x30 };
            client.SendPacket(client_packet, (client_packet.Length));

            byte[] server_packet = { 0x01, 0x11, 0x21, 0x31 };
            server.SendPacket(server_packet, (server_packet.Length));

            while (true)
            {
                byte[] packet = new byte[256];
                int bytes_read = client.ReceivePacket(ref packet, (packet.Length));
                if (bytes_read == 0)
                    break;
            }

            while (true)
            {
                byte[] packet = new byte[256];
                int bytes_read = server.ReceivePacket(ref packet, (packet.Length));
                if (bytes_read == 0)
                    break;
            }

            client.Update(DeltaTime);
            server.Update(DeltaTime);
        }

        if (!client.IsConnected()) { Debug.LogError("client connect error"); };
        if (!server.IsConnected()) { Debug.LogError("server connect error"); };
    }
    void Start()
    {
        test_join();
    }
    void Update()
    {

    }
}