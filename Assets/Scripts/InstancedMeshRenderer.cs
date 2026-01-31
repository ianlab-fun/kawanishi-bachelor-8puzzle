using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;

public class InstancedMeshRenderer : IDisposable
{
    private static readonly int MatricesProperty = Shader.PropertyToID("_Matrices");

    private InstancedMeshInfo _meshInfo;
    private List<Matrix4x4> _matrices;
    private bool _requiresFullUpdate;
    private bool _isPartialUpdateEnabled;

    private GraphicsBuffer _matrixBuffer;
    private RenderParams _renderParams;
    private int _instanceCount;
    private bool _isInitialized;

    public InstancedMeshRenderer(InstancedMeshInfo meshInfo, int initialCapacity = 0)
    {
        _meshInfo = meshInfo;
        _matrices = initialCapacity > 0
            ? new List<Matrix4x4>(initialCapacity)
            : new List<Matrix4x4>();
        _requiresFullUpdate = false;
        _isPartialUpdateEnabled = true;
        _isInitialized = false;
    }

    public void AddMatrix(Matrix4x4 matrix)
    {
        _matrices.Add(matrix);
    }

    public void AddMatrices(IEnumerable<Matrix4x4> matrices)
    {
        _matrices.AddRange(matrices);
    }

    public void ClearMatrices()
    {
        _matrices.Clear();
        _instanceCount = 0;
        if (_matrixBuffer != null)
        {
            _matrixBuffer.Release();
            _matrixBuffer = null;
        }
        _isInitialized = false;
    }
    
    /// <summary>
    /// マテリアルプロパティのfloat値を設定する
    /// </summary>
    public void SetFloat(int propertyId, float value)
    {
        if (!_isInitialized) return;
        _renderParams.matProps.SetFloat(propertyId, value);
    }
    
    public void ApplyMatrixData()
    {
        _instanceCount = _matrices.Count;

        if (_matrixBuffer != null && _matrixBuffer.count != _instanceCount)
        {
            _matrixBuffer.Release();
            _matrixBuffer = null;
        }

        if (_instanceCount <= 0) return;
        
        _matrixBuffer ??= new GraphicsBuffer(GraphicsBuffer.Target.Structured, _instanceCount, Marshal.SizeOf<Matrix4x4>());
        _matrixBuffer.SetData(_matrices);

        if (!_isInitialized)
        {
            _renderParams = new RenderParams(_meshInfo.mat);
            _renderParams.matProps = new MaterialPropertyBlock();
            _renderParams.worldBounds = new Bounds(Vector3.zero, new Vector3(60000f, 60000f, 60000f));
            _isInitialized = true;
        }
        _renderParams.matProps.SetBuffer(MatricesProperty, _matrixBuffer);
    }
    
    public void Render()
    {
        if (_instanceCount == 0 || _meshInfo.mesh == null || !_isInitialized)
        {
            return;
        }

        Graphics.RenderMeshPrimitives(
            _renderParams,
            _meshInfo.mesh,
            0,
            _instanceCount
        );
    }

    public void Dispose()
    {
        _matrixBuffer?.Release();
        _matrixBuffer = null;
    }
}