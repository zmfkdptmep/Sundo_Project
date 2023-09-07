package egovframework.example.sample.service.impl;

import java.util.List;

import org.egovframe.rte.psl.dataaccess.mapper.Mapper;

import egovframework.example.sample.service.CarVO;
import egovframework.example.sample.service.CleanVO;

@Mapper("carMapper")
public interface CarMapper {
	
	public List<CarVO> getCarList();
	
	public CleanVO getClean(CleanVO vo);
	
	

}
