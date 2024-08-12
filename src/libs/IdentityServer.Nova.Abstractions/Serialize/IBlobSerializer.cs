﻿namespace IdentityServer.Nova.Abstractions.Serialize;

public interface IBlobSerializer
{
    string SerializeObject(object obj);
    T DeserializeObject<T>(string text);
}