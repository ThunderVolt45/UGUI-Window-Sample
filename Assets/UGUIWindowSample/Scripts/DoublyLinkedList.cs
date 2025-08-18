using System;
using System.Collections;
using System.Collections.Generic;

#nullable enable
/// <summary>
/// 이중 연결 리스트의 각 요소를 나타내는 노드 클래스입니다.
/// </summary>
/// <typeparam name="T">노드가 저장할 데이터의 타입</typeparam>
public class Node<T>
{
    public T Value { get; set; }
    public Node<T>? Next { get; internal set; }
    public Node<T>? Previous { get; internal set; }

    public Node(T value)
    {
        this.Value = value;
        this.Next = null;
        this.Previous = null;
    }
}

/// <summary>
/// 제네릭 이중 연결 리스트 클래스입니다.
/// </summary>
/// <typeparam name="T">리스트가 저장할 데이터의 타입</typeparam>
public class DoublyLinkedList<T> : IEnumerable<T>
{
    public Node<T>? Head { get; private set; }
    public Node<T>? Tail { get; private set; }
    public int Count { get; private set; }

    public DoublyLinkedList()
    {
        Head = null;
        Tail = null;
        Count = 0;
    }

    /// <summary>
    /// 리스트의 맨 앞에 노드를 추가합니다.
    /// </summary>
    public void AddFirst(T value)
    {
        Node<T> newNode = new Node<T>(value);
        if (Head == null)
        {
            Head = newNode;
            Tail = newNode;
        }
        else
        {
            newNode.Next = Head;
            Head.Previous = newNode;
            Head = newNode;
        }
        Count++;
    }

    /// <summary>
    /// 리스트의 맨 뒤에 노드를 추가합니다.
    /// </summary>
    public void AddLast(T value)
    {
        if (Head == null)
        {
            AddFirst(value);
            return;
        }

        Node<T> newNode = new Node<T>(value);
        Tail!.Next = newNode;
        newNode.Previous = Tail;
        Tail = newNode;
        Count++;
    }

    /// <summary>
    /// 특정 값을 가진 노드를 리스트에서 제거합니다.
    /// </summary>
    /// <returns>성공하면 true, 실패하면 false</returns>
    public bool Remove(T value)
    {
        Node<T>? currentNode = Head;

        // 삭제할 노드를 찾습니다.
        while (currentNode != null && !EqualityComparer<T>.Default.Equals(currentNode.Value, value))
        {
            currentNode = currentNode.Next;
        }

        if (currentNode == null)
        {
            return false; // 노드를 찾지 못함
        }

        // 노드를 제거합니다.
        RemoveNode(currentNode);
        return true;
    }

    /// <summary>
    /// 특정 노드 참조를 받아 리스트에서 제거합니다.
    /// </summary>
    public void RemoveNode(Node<T> nodeToRemove)
    {
        if (nodeToRemove == null)
        {
            throw new ArgumentNullException(nameof(nodeToRemove));
        }

        // 이전 노드의 Next 포인터를 수정
        if (nodeToRemove.Previous != null)
        {
            nodeToRemove.Previous.Next = nodeToRemove.Next;
        }
        else // 삭제할 노드가 Head인 경우
        {
            Head = nodeToRemove.Next;
        }

        // 다음 노드의 Previous 포인터를 수정
        if (nodeToRemove.Next != null)
        {
            nodeToRemove.Next.Previous = nodeToRemove.Previous;
        }
        else // 삭제할 노드가 Tail인 경우
        {
            Tail = nodeToRemove.Previous;
        }

        Count--;
    }

    // foreach 구문을 지원하기 위한 IEnumerator 구현
    public IEnumerator<T> GetEnumerator()
    {
        Node<T>? current = Head;
        while (current != null)
        {
            yield return current.Value;
            current = current.Next;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}