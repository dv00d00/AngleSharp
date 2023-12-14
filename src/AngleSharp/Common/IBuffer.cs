﻿namespace AngleSharp.Common;

using System;

/// <summary>
///
/// </summary>
public ref struct StringOrMemory
{
    private String? _string;
    private readonly ReadOnlyMemory<Char> _memory;

    /// <summary>
    ///
    /// </summary>
    /// <param name="str"></param>
    public StringOrMemory(String str)
    {
        _memory = str.AsMemory();
        _string = str;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="memory"></param>
    public StringOrMemory(ReadOnlyMemory<Char> memory)
    {
        _memory = memory;
        _string = null;
    }

    /// <summary>
    ///
    /// </summary>
    public readonly ReadOnlyMemory<Char> Memory => _memory;

    /// <summary>
    ///
    /// </summary>
    public String String => _string ??= _memory.Span.CreateString();
}

/// <summary>
///
/// </summary>
public interface IBuffer : IDisposable
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    IBuffer Append(Char c);

    /// <summary>
    ///
    /// </summary>
    /// <returns></returns>
    IBuffer Clear();

    /// <summary>
    ///
    /// </summary>
    Int32 Length { get; }

    /// <summary>
    ///
    /// </summary>
    Int32 Capacity { get; }

    /// <summary>
    ///
    /// </summary>
    /// <param name="start"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    IBuffer Remove(Int32 start, Int32 length);

    /// <summary>
    ///
    /// </summary>
    /// <param name="offset"></param>
    /// <param name="dest"></param>
    /// <param name="length"></param>
    void CopyTo(Int32 offset, Span<Char> dest, Int32 length);

    /// <summary>
    ///
    /// </summary>
    void ReturnToPool();

    /// <summary>
    ///
    /// </summary>
    /// <param name="index"></param>
    /// <param name="c"></param>
    /// <returns></returns>
    IBuffer Insert(Int32 index, Char c);

    /// <summary>
    ///
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    IBuffer Append(ReadOnlySpan<Char> str);

    /// <summary>
    /// Gets the character at the specified index.
    /// </summary>
    /// <param name="i"></param>
    Char this[Int32 i] { get; }

    /// <summary>
    /// Gets the data as a memory.
    /// </summary>
    /// <returns></returns>
    StringOrMemory GetData();
}