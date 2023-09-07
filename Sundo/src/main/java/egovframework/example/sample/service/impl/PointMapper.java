package egovframework.example.sample.service.impl;

import org.egovframe.rte.psl.dataaccess.mapper.Mapper;

import egovframework.example.sample.service.PointVO;

@Mapper("pointMapper")
public interface PointMapper {
	
	
	public int insertPoint(PointVO vo);

}
