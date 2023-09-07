package egovframework.example.sample.service.impl;

import java.util.List;

import javax.annotation.Resource;

import org.egovframe.rte.fdl.cmmn.EgovAbstractServiceImpl;
import org.springframework.stereotype.Service;

import egovframework.example.sample.service.CarService;
import egovframework.example.sample.service.CarVO;
import egovframework.example.sample.service.CleanVO;

@Service("carService")
public class CarServiceImpl extends EgovAbstractServiceImpl implements CarService {
	
	
	@Resource(name = "carMapper")
	private CarMapper mapper;

	@Override
	public List<CarVO> getCarList() {
		return mapper.getCarList();
	}

	@Override
	public CleanVO getClean(CleanVO vo) {
		return mapper.getClean(vo);
	}
}
