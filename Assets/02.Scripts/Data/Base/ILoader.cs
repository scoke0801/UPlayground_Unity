using System.Collections.Generic;

public interface ILoader<Key, Value>
{
    Key GetKey();
}

// JSON 배열 파싱을 위한 헬퍼 클래스
[System.Serializable]
public class Wrapper<T>
{
    public List<T> dataList;
}