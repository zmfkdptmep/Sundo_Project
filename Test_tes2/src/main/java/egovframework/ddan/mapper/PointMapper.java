package egovframework.ddan.mapper;

import java.util.List;

import org.apache.ibatis.annotations.Param;
import org.egovframe.rte.psl.dataaccess.mapper.Mapper;

import egovframework.ddan.service.MemberVo;
import egovframework.ddan.vo.CarVo;
import egovframework.ddan.vo.CleanVo;
import egovframework.ddan.vo.CsvVO;
import egovframework.ddan.vo.LocationVo;
import egovframework.ddan.vo.PointsVo;

@Mapper("pMapper")
public interface PointMapper {
	
	
	public int insertList(LocationVo vo);
	
	public List<PointsVo> getCarList();

	public CleanVo getCleanData(PointsVo points);
	
	public int insertCsv(CsvVO vo);
	
	public List<CleanVo> getStaics(PointsVo poitns);
	
	public CleanVo monthData();
	
	public List<PointsVo> getDateList(String car_num);
	
	public CleanVo getCleanTimeRatio(@Param("date") String date, @Param("car_num") String car_num);
	public PointsVo getCenter(@Param("date") String date, @Param("car_num") String car_num);
	
	public List<PointsVo> goLive();

	public MemberVo login(MemberVo member);

	public int addCar(CarVo carVo);
}
