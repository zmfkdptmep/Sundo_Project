package egovframework.example.sample.service.impl;


import org.egovframe.rte.psl.dataaccess.mapper.Mapper;

import egovframework.example.sample.service.TempVO;

@Mapper("tempMapper")
public interface TempMapper {
	
	
	public int insertTemp(TempVO vo);
	
	public int deleteTemp();
	
	public int countTemp();
}
