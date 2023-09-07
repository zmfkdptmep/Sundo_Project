package egovframework.example.sample.service.impl;

import java.util.List;

import org.egovframe.rte.psl.dataaccess.mapper.Mapper;

import egovframework.example.sample.service.CoopVO;

@Mapper("coopMapper")
public interface CoopMapper {
	
	
	public List<CoopVO> getCoopList();
	public int insertCoop();

}
