package VCS::Interceptor;
use strict;

use VCS::Config;
use LWP;
use HTTP::Request;
use Digest::MD5  qw(md5_hex);
use Data::Dumper;
use Encode qw(decode encode);


sub new
# //////////////////////////////////////////////////
{
	my ( $class, $pclass, $vars ) = @_;
	
	my $self = bless {}, $pclass;
	
	$self->{ 'VCS::Vars' } = $vars;
	
	return $self;
}

sub send_connection_signal
# //////////////////////////////////////////////////
{
	my ( $self, $interceptor ) = @_;

	my $vars = $self->{ 'VCS::Vars' };
	
	my $request = '<?xml version="1.0" encoding="UTF-8"?>' . 
		'<toCashbox>' . 
			'<CheckConnection>MakeBeep</CheckConnection>' .
			'<Info><Cashier>' . $vars->get_session->{'login'} . '</Cashier></Info>' .
		'</toCashbox>';
	
	return send_request( $vars, $request, $interceptor );
}	

sub send_docpack
# //////////////////////////////////////////////////
{

	my ( $self, $docid, $ptype, $summ, $data, $login, $pass, $callback, $r, $sh_return ) = @_;

	my $vars = $self->{ 'VCS::Vars' };
	
	if ( !$data ) {
	
		my $docobj = VCS::Docs::docs->new( 'VCS::Docs::docs', $vars );
		my $error = $docobj->getDocData( \$data, $docid, 'individuals' );
	}	
	
	return ( "ERR3", "Договор юрлица не может быть оплачен" ) if $data->{ jurid };
	
	$login = $vars->get_session->{'login'} unless $login;
	
	my $pass = '';
	
	if ( !$callback && !$pass ) {
	
		return ( "ERR3", "Неверные настройки данных подключения" ) unless $vars->get_session->{ interceptor };
		
		$pass = $vars->db->sel1("
			SELECT CashPassword FROM Cashboxes_interceptors WHERE ID = ?", $vars->get_session->{ interceptor }
		);
		
	}
	elsif ( !$pass ) {
	
		$pass = $vars->db->sel1("
			SELECT CashPassword FROM Cashboxes_interceptors WHERE InterceptorIP = ?", $callback
		);
	}
	
	return ( "ERR3", "Неверный кассовый пароль в настройках" ) unless $pass;
	
	my ( $services, $services_fail ) = doc_services( $self, $data, $ptype, $summ, $login, $pass, $callback, $r, $sh_return );

	my $request = xml_create( $services->{ services }, $services->{ info } );

	my $resp = send_request( $vars, $request, undef, $callback );
	
	$vars->db->query("
		UPDATE Cashboxes_interceptors SET LastUse = now(), LastResponse = ? WHERE ID = ?", {},
		$resp, $vars->get_session->{ interceptor }
	);
	
	return split( /:/, $resp ), $services_fail;
}

sub xml_create
# //////////////////////////////////////////////////
{
	my ( $services, $info ) = @_;
	
	my ( $md5line, $bytecode ) = ( '', '' );
	
	my $xml = '<Services>';
	
	for my $service ( keys %$services ) {
	
		$xml .= '<Service>';
	
		for my $field ( sort { $a cmp $b } keys %{ $services->{ $service } } ) {
		
			$xml .= "<$field>" . $services->{ $service }->{ $field } . "</$field>";
			
			$md5line .= $services->{ $service }->{ $field };
		}
		
		$xml .= '</Service>';
	}
	
	$xml .= '</Services>';
	
	$xml .= '<Info>';
	
	for ( sort { $a cmp $b } keys %$info ) {
	
		$xml .= "<$_>" . $info->{ $_ } . "</$_>";
		
		$md5line .= $info->{ $_ };
	}
	
	$xml .= '</Info>';
	
	my $currentDate = get_date();
	
	$md5line .= $currentDate;
	
	Encode::from_to( $md5line, 'utf-8', 'windows-1251' );

	$bytecode .= ord( $_ ) . " " for split( //, $md5line );
	
	my $md5 = Digest::MD5->new->add( $bytecode )->hexdigest;
	
	$xml =  '<?xml version="1.0" encoding="UTF-8"?>' . 
		'<toCashbox>' . 
			$xml .
			'<Control>' .
				'<CRC>' . $md5 . '</CRC>' . 
				'<Date>' . $currentDate . '</Date>' .
				
			'</Control>' .
		'</toCashbox>';

	return $xml;
}

sub send_request
# //////////////////////////////////////////////////
{
	my ( $vars, $line, $interceptor, $callback ) = @_;
	
	my $serv;
	
	if ( !$callback ) {
	
		$interceptor = $vars->get_session->{ interceptor } unless $interceptor;
		
		$serv = $vars->db->sel1("
			SELECT InterceptorIP FROM Cashboxes_interceptors WHERE ID = ?",
			$interceptor
		);
	}
	else {
		$serv = $callback;
	}
	
	return "ERR4:Не установлена кассовая интеграция" unless $serv =~ /^([0-9]{1,3}[\.]){3}[0-9]{1,3}$/;
	
	my $ua = LWP::UserAgent->new;
	
	$ua->agent( 'Mozilla/4.0 (compatible; MSIE 6.0; X11; Linux i686; en) Opera 7.60' );

	my $request = HTTP::Request->new( GET => 'http://'.$serv.'/?message='.$line.';' );

	my $response = $ua->request( $request );

	return "ERR1:Нет связи с кассовой программой (она не запущена?)" if $response->{ _rc } != 200;
	
	return $response->{ _content };
}

sub get_date
# //////////////////////////////////////////////////
{
	my ( $sec, $min, $hour, $mday, $mon, $year, $wday, $yday, $isdst ) = localtime( time );
	
	$year += 1900;
	
	$mon++;
	
	for ( $sec, $min, $hour, $mday, $mon, $year ) { 
	
		$_ = '0' . $_ if $_ < 10;
	};
	
	return "$year-$mon-$mday $hour:$min:$sec";
}

sub get_all_add_services
# //////////////////////////////////////////////////
{
	my ( $self, $vars, $data, $callback ) = @_;
	
	my $services = [];

	my $serv_price = $vars->db->selallkeys("
		SELECT ServiceID, Price FROM ServicesPriceRates WHERE PriceRateID = ?",
		$data->{ rate }
	);
	
	my %serv_price = map { $_->{ ServiceID } => $_->{ Price } } @$serv_price;

	if ( !$callback ) {

		$services = $vars->db->selallkeys("
			SELECT Services.ID as ServiceID, Services.Name, DocPackService.ServiceID,
			ServiceFields.ValueType, ServiceFieldValuesINT.Value
			FROM DocPackService
			JOIN Services ON DocPackService.ServiceID = Services.ID
			JOIN ServiceFields ON DocPackService.ServiceID = ServiceFields.ServiceID
			JOIN ServiceFieldValuesINT ON ServiceFieldValuesINT.DocPackServiceID = DocPackService.ID
			WHERE PackID = ?
			GROUP BY DocPackServiceID",
			$data->{ docid }
		);
	}
	else {
		my $serv_names = $vars->db->selallkeys( "SELECT ID, Name FROM Services" );
		
		my %services_name = map { $_->{ ID } => $_->{ Name } } @$serv_names;

		for ( sort { $a <=> $b } keys %services_name ) {
		
			if ( $data->{ "srv$_" } ) {

				my $value = ( $_ == 1 ? $serv_price{ $_ } : $data->{ "srv$_" } ) || 0;
				
				push @$services, {
				
					ServiceID 	=> $_,
					Name 		=> $services_name{ $_ },
					Value 		=> $value,
					ValueType 	=> ( $_ == 1 ? 1 : 2 ),
				};
			}
		}
	}

	my $serv_list = {};
	
	for ( @$services ) {
	
		if ( $_->{ ValueType } == 1 ) {
			
			$serv_list->{ "service" . $_->{ ServiceID } } = {
			
				Name		=> $_->{ Name },
				Quantity	=> 1,
				Price		=> $_->{ Value },
				VAT 		=> 1,
				Department	=> 1,
			};
		}
		elsif ( $_->{ ValueType } == 2 ) {
		
			my $price = $serv_price{ $_->{ ServiceID } } || 0;
		
			$serv_list->{ "service" . $_->{ ServiceID } } = {
			
				Name		=> $_->{ Name },
				Quantity	=> $_->{ Value },
				Price		=> $price,
				VAT 		=> 1,
				Department	=> 1,
			};
		};
	}

	return $serv_list;
}

sub get_service_code
# //////////////////////////////////////////////////
{
	my ( $self, $serv, $center, $urgance, $ord ) = @_;
	
	my $country = '(ITA';
		
	my $center_id = {
		1	=> '00', # MSK
		2	=> '01',
		9	=> '02',
		8	=> '03',
		3	=> '04',
		5	=> '05',
		7	=> '06',
		6	=> '07',
		4	=> '08',
		12	=> '10',
		13	=> '11',
		14	=> '12',
		15	=> '14',
		16	=> '15',
		18	=> '16',
		17	=> '17',
		19	=> '18',
		20	=> '19',
		22	=> '20',
		21	=> '21',
		23	=> '22',
		25	=> '25',
		24	=> '26',
		26	=> '27',
		31	=> '91', # VIP
		32	=> '91', # TP
		40	=> '92', # VIP MSK K
		41	=> '92', # MSK PT
		44	=> '92', # MSK K
		45	=> '92', # MSK VIP C
	};
	
	my $serv_group = {
		'shipping'	=> '501',
		'sms'		=> '300',
		'tran'		=> '000',
		'xerox'		=> '400',
		'ank'		=> '502',
		'print'		=> '503',
		'photo'		=> '504',
		'vip'		=> '505',

		'service1' 	=> '506',
		'service2' 	=> '704',
		'service3' 	=> '000',
		'service4' 	=> '705',
		'service5' 	=> '511',
		'service6' 	=> '512',
		'service7' 	=> '513',
		'service8' 	=> '514',
		'service9' 	=> '515',
		'service10' 	=> '000',
	};
	
	if ( $serv eq 'visa' ) {
	
		my $pay_type = 1;
		
		my $urgance_code = ( $urgance ? '1' : '0' );
		
		my $agent = 1;
		
		return $country . $center_id->{ $center } . $pay_type . $urgance_code . $agent . ') ';
	}
	else {
	
		return $country . $center_id->{ $center } . $serv_group->{ $serv } . ') ';
	}
}

sub doc_services
# //////////////////////////////////////////////////
{
	my ( $self, $data, $ptype, $summ, $login, $pass, $callback, $reception, $sh_return ) = @_;

	my $vars = $self->{'VCS::Vars'};

	my ( $cntres, $cntnres, $cntncon, $cntage, $smscnt, $shcnt, $shrows, $shind, $indexes, $dhlsum, $inssum, $inscnt ) = 
		( 0, 0, 0, 0, 0, 0, {}, '', {}, 0, 0, 0 );

	if ( $data->{ shipping } == 1 ) {

		$dhlsum = $data->{ shipsum };
		$shcnt = 1;
	}

	my ( $apcnt, $astr, $bankid, $prevbank ) = ( 0, '', '', 0 );
	
	for my $ak ( @{ $data->{ applicants } } ) {

		next if $ak->{ Status } == 7;

		$apcnt++ unless $data->{ direct_docpack } and $ak->{ Status } != 1;
		
		if ( !$data->{ direct_docpack } or $ak->{ direct_concil } ) {
		
			if ( $ak->{ Concil } ) {
			
				$cntncon++;
			}
			else {
				if ( $ak->{ AgeCatA } ) {
				
					$cntage++;
				}
				else {
					if ( $ak->{ iNRes } ) {
					
						$cntnres++;
					}
					else {					
						$cntres++;
					}
				}
			}
		}

		if ( ( $data->{ sms_status } == 2 ) && ( $ak->{ MobileNums } ne '' ) ) {
		
			$smscnt++;
		}
		
		if ( ( $data->{ shipping } == 2 ) && ( $ak->{ ShipAddress } ne '' ) ) {
		
			$shcnt++;
			$dhlsum += $ak->{ RTShipSum };
		}
	}
	
	my ( $agesfree, $ages ) = $vars->db->sel1("
		SELECT AgesFree, Ages FROM PriceRate WHERE ID = ?",
		$data->{ rate }
	);
	
	$data->{ concilPaymentDate } =~ /(\d{2})\.(\d{2})\.(\d{4})/;

	my $concil_payment_date = "$3-$2-$1";
	
	my $prices = $vars->admfunc->getPrices( $vars, $data->{ rate }, $data->{ vtype }, $concil_payment_date );
	
	my $insurance = $data->{ insurance_manual_service } || '0.00';
	
	my $shsum = 0;
	
	if ( $data->{ newdhl } ) {
	
		$shsum = sprintf( "%.2f", $dhlsum );
	}
	else {
		$shsum = sprintf( "%.2f", $prices->{ shipping } * $shcnt );
	}
	
	$smscnt = 1 if $data->{ sms_status } == 1;
	
	my $vprice = ( $data->{ urgent } ? $prices->{ 'urgent' } : $prices->{ 'visa' } );
	
	my $special_department = ( $reception ? 3 : 1 );
	
	my $servsums = {
		shipping => {
			Name		=> 'Услуги по доставке документов на дом',
			Quantity	=> $shcnt,
			Price		=> sprintf( "%.2f", $shsum ),
			VAT		=> 1,
			Department	=> 1,
			Shipping	=> 1,
			ReturnShipping	=> ( $sh_return ? 1 : 0 ),
		},
		sms => {
			Name		=> ' Услуги по оповещению (СМС сообщение)',
			Quantity	=> $smscnt,
			Price		=> sprintf( "%.2f", $prices->{ sms } ),
			VAT		=> 1,
			Department	=> 1,
		},
		tran => {
			Name		=> 'Услуги по переводу документов',
			Quantity	=> $data->{ transum },
			Price		=> sprintf( "%.2f", $prices->{ tran } ),
			VAT		=> 1,
			Department	=> 1,
		},
		xerox => {
			Name		=> 'Услуги по копированию документов',
			Quantity	=> $data->{ xerox },
			Price		=> sprintf( "%.2f", $prices->{ xerox } ),
			VAT		=> 1,
			Department	=> $special_department,
			ReceptionID	=> 1,
		},
		visa => {
			Name		=> 'Услуги по оформлению документов',
			Quantity	=> $apcnt,
			Price		=> sprintf( "%.2f", $vprice ),
			VAT		=> 1,
			Department	=> 1,
		},
		ank => {
			Name		=> 'Услуги по заполнению и распечатке анкеты' . ( $reception ? '' : ' заявителя' ),
			Quantity	=> $data->{ anketasrv },
			Price		=> sprintf( "%.2f", $prices->{ anketasrv } ),
			VAT		=> 1,
			Department	=> $special_department,
			ReceptionID	=> 2,
		},
		print => {
			Name		=> 'Услуги по распечатке документов',
			Quantity	=> $data->{ printsrv },
			Price		=> sprintf( "%.2f", $prices->{ printsrv } ),
			VAT		=> 1,
			Department	=> $special_department,
			ReceptionID	=> 3,
		},
		photo => {
			Name		=> 'Услуги фотосъемке и изготовлению фото',
			Quantity	=> $data->{ photosrv },
			Price		=> sprintf( "%.2f", $prices->{ photosrv } ),
			VAT		=> 1,
			Department	=> $special_department,
			ReceptionID	=> 4,
		},
		vip => {
			Name		=> 'Услуги по ВИП обслуживанию',
			Quantity	=> $data->{ vipsrv },
			Price		=> sprintf( "%.2f", $prices->{ vipsrv } ),
			VAT		=> 1,
			Department	=> 1,
		},
		insurance => {
			Name		=> 'Страховка',
			Quantity	=> ( $data->{ insurance_manual_service } ? 1 : 0 ),
			Price		=> sprintf( "%.2f", $insurance ),
			VAT		=> 0,
			Department	=> 4,
			WithoutServCode	=> 1,
		},
	};
	
	my $concil_price = {};
	my $u = ( $data->{ urgent } ? 'u' : '' );
	
	concil_add_price( $concil_price, $prices, "concilr$u", $cntres ) if $data->{ vcat } eq 'C';
	concil_add_price( $concil_price, $prices, "conciln$u", $cntnres );
	concil_add_price( $concil_price, $prices, "concilr$u" . '_' . $ages, $cntage );
	concil_add_price( $concil_price, $prices, "concilr$u", $cntres ) if $data->{ vcat } eq 'D';

	my $consil_index = 0;
	
	my $concil_price_to_name = {
		35  => 'C01',
		50  => 'D05',
		60  => 'C03',
		70  => 'C02',
		116 => 'D04',
	};
	
	for ( keys %$concil_price ) {
	
		$consil_index += 1;
		
		my $concil_name = $concil_price_to_name->{ $concil_price->{ $_ }->{ type } } || '';
	
		$servsums->{ "concil$consil_index" } = {
			Name		=> "Консульский сбор $concil_name",
			Quantity	=> $concil_price->{ $_ }->{ cnt },
			Price		=> $_,
			VAT		=> 0,
			Department	=> 2,
			WithoutServCode	=> 1,
		};
	}
	
	my $serv_hash = get_all_add_services( $self, $vars, $data, $callback );
	
	for ( keys %$serv_hash ) {
	
		$servsums->{ $_ } = $serv_hash->{ $_ };
	}

	my ( $total, $mandocpack_failserv, $ord ) = ( 0, '', 1 );

	for my $serv ( keys %$servsums ) {
	
		$servsums->{ $serv }->{ Name } =
			get_service_code( $self, $serv, $data->{ center }, $data->{ urgent }, $ord ) . $servsums->{ $serv }->{ Name }
				unless $servsums->{ $serv }->{ WithoutServCode };
	
		$mandocpack_failserv .= ( $mandocpack_failserv ? ', ' : '' ) . $servsums->{ $serv }->{ Name }
			if $servsums->{ $serv }->{ Quantity }
				and (
					$servsums->{ $serv }->{ Price } eq '0.00'
					or
					!$servsums->{ $serv }->{ Price }
				);

		if ( 
			(
				!$servsums->{ $serv }->{ Quantity } 
				or
				$servsums->{ $serv }->{ Price } eq '0.00' or !$servsums->{ $serv }->{ Price }
			) or (
				$sh_return && !$servsums->{ $serv }->{ Shipping }
			)
		) {
		
			delete $servsums->{ $serv };
		}
		else {
		
			$ord += 1;
		
			$total += $servsums->{ $serv }->{ Price } * $servsums->{ $serv }->{ Quantity };
		}
	}

	my $info = {
		AgrNumber => $data->{ docnum },
		Cashier => $login,
		CashierPass => $pass,
		MoneyType => $ptype,
		Total => $total,
		Money => $summ,
		RequestOnly => ( $callback ? '1' : '0' ),
	};

	return { services => $servsums, info => $info }, $mandocpack_failserv;
}

sub concil_add_price
# //////////////////////////////////////////////////
{
	my ( $concil_price, $prices, $price_type, $cnt ) = @_;

	my $current_cnt;
	
	if ( ref( $concil_price->{ sprintf( '%.2f', $prices->{ $price_type } ) } ) eq 'HASH' ) {
	
		$current_cnt = $concil_price->{ sprintf( '%.2f', $prices->{ $price_type } ) }->{ cnt } || 0;
	}
	else {
		$current_cnt = 0;
		$concil_price->{ sprintf( '%.2f', $prices->{ $price_type } ) } = {};
	}
	
	$concil_price->{ sprintf( '%.2f', $prices->{ $price_type } ) }->{ cnt } = $current_cnt + $cnt;
	
	$concil_price->{ sprintf( '%.2f', $prices->{ $price_type } ) }->{ type } = $prices->{ 'o' . $price_type };
	
	return $concil_price;
}

sub cash_box_output_error_check
# //////////////////////////////////////////////////
{
	my ( $self, $code, $desc ) = @_;

	if ( $code ne 'OK' ) {
	
		my $err_type = "(неизвестная ошибка)";
	
		$err_type = "(ошибка кассовой программы)" if $code =~ /^ERR1$/;
		$err_type = "(ошибка кассы)" if $code =~ /^ERR2$/;
		$err_type = "(ошибка настроек)" if $code =~ /^ERR(3|4)$/;
	
		return cash_box_output( $self, "ERROR|$err_type $desc" );
	}
	
	return cash_box_output( $self, "OK|$desc" );
}

sub cash_box_ship_return
# //////////////////////////////////////////////////
{
	my $self = shift;
	
	my $vars = $self->{'VCS::Vars'};

	my $docid = $vars->getparam( 'docid' ) || 0;
	
	return cash_box_output( $self, "ERROR|Ошибка параметров" )
		unless $docid;
	
	my ( $ptype, $summ ) = $vars->db->sel1("
		SELECT PType, TShipSum FROM DocPack WHERE ID = ?",
		$docid
	);
	
	my ( $code, $desc, undef ) = send_docpack( $self, $docid, $ptype, $summ, undef, undef, undef, undef, undef, 'sh_return' );
	
	cash_box_output_error_check( $self, $code, $desc );
}

sub cash_box
# //////////////////////////////////////////////////
{
	my $self = shift;
	
	my $vars = $self->{'VCS::Vars'};
	
	my $param = {};
	
	$param->{ $_ } = ( $vars->getparam( $_ ) || 0 ) for ( 'docid', 'ptype', 'summ' );
	
	$param->{ $_ } =~ s/[^0-9]//g for ( 'docid', 'ptype' );
	
	$param->{ summ } =~ s/,/./g if $param->{ summ } =~ /,/;
	
	return cash_box_output( $self, "ERROR|Недопустимые символы в поле суммы" )
		if $param->{ summ } =~ /[^0-9\.]/;
	
	return cash_box_output( $self, "ERROR|Ошибка параметров" )
		if !$param->{ docid } or ( $param->{ ptype } != 1 and $param->{ ptype } != 2 );
	
	return cash_box_output( $self, "ERROR|Не введена сумма оплаты" )
		if !$param->{ summ } and $param->{ ptype } == 1;	
	
	my ( $code, $desc, undef ) = send_docpack( $self, $param->{ docid }, $param->{ ptype }, $param->{ summ } );
	
	cash_box_output_error_check( $self, $code, $desc );
}

sub cash_box_auth
# //////////////////////////////////////////////////
{
	my ( $self, $task, $id, $template, $slist, $authip, $clientip ) = @_;
	
	my $vars = $self->{'VCS::Vars'};

	my $param = {};
	
	$param->{ $_ } = ( $vars->getparam( $_ ) || '' ) for ( 'login', 'p', 'ip', 'v' );

	return cash_box_output( $self, "ERROR|Укажите данные для авторизации" ) if !$param->{ login } or !$param->{ p };
	
	my ( $login, $name, $surname, $secname ) = $vars->db->sel1("
		SELECT Login, UserName, UserLName, UserSName
		FROM Users
		WHERE Login = ? AND Pass = ? AND
		(RoleID = 8 OR RoleID = 5 OR RoleID = 2)
		AND Locked = 0",
		$param->{ login }, $param->{ p }
	);

	return cash_box_output( $self, "ERROR|Неудачная авторизация в VMS" ) unless $login;
	
	return cash_box_output( $self, "ERROR|Неверные данные IP-адреса" ) unless $param->{ ip } =~ /^([0-9]{1,3}[\.]){3}[0-9]{1,3}$/;
		
	my $pass = $vars->db->sel1("
		SELECT CashPassword FROM Cashboxes_interceptors WHERE InterceptorIP = ?", $param->{ ip }
	);
	
	return cash_box_output( $self, "ERROR|IP-адрес не найден в БД или не установлен кассовый пароль" ) unless $pass;
	
	$vars->db->query("
		UPDATE Cashboxes_interceptors SET LastVersion = ? WHERE InterceptorIP = ?", {},
		$param->{ v }, $param->{ ip }
	);
	
	return cash_box_output( $self, "OK|$pass|$surname $name $secname" );
}

sub cash_box_centers
# //////////////////////////////////////////////////
{
	my ( $self, $task, $id, $template, $slist, $authip, $clientip ) = @_;
	
	my $vars = $self->{'VCS::Vars'};
	
	my $login = $vars->getparam( 'login' ) || undef;
	
	return cash_box_output( $self, "" ) unless $login;
	
	my $centers_line = $vars->db->sel1("
		SELECT branches FROM Users WHERE Login = ?", $login
	);
	
	my $centers_array = $vars->db->selallkeys("
		SELECT BName FROM Branches WHERE ID in ($centers_line)"
	);
	
	return cash_box_output( $self, "" ) if @$centers_array == 0;
	
	my $centers_name = join ( '|', map { $_->{ BName } } @$centers_array );
	
	return cash_box_output( $self, $centers_name );
}

sub cash_box_vtype
# //////////////////////////////////////////////////
{
	my ( $self, $task, $id, $template, $slist, $authip, $clientip ) = @_;
	
	my $vars = $self->{'VCS::Vars'};
	
	my $center = $vars->getparam( 'center' ) || '';
	
	return cash_box_output( $self, "" ) unless $center;
	
	my $vtypes_array = $vars->db->selallkeys("
		SELECT VName, Centers FROM VisaTypes"
	);
		
	my $center_id = $vars->db->sel1("
		SELECT ID FROM Branches WHERE BName = ?", $center
	);
		
	my $vtypes = '';
	
	for ( @$vtypes_array ) {
	
		my %centers = map { $_ => 1 } split( /,/, $_->{ Centers } );

		$vtypes .= ( $vtypes ? '|' : '' ) . $_->{ VName } if exists $centers{ $center_id };
	};
	
	return cash_box_output( $self, $vtypes );
}

sub download_receipt
{
	my ( $self, $task, $id, $template ) = @_;
	
	my $vars = $self->{'VCS::Vars'};
	
	my $gconfig = $vars->getConfig( 'general' );
	
	my $file_id = $vars->getparam('rid');
	
	my $file = $vars->db->selallkeys("
		SELECT CRC, OriginalName FROM Cashboxes_receipt 
		WHERE ID = ?", $file_id
	)->[0];
   
	my $file_path = $gconfig->{ tmp_folder } . "cashbox_receipt/" . $file->{ CRC };

	my $content = '';
	
	open my $filefile, '<', $file_path or die;
	
	while ( <$filefile> ) {
		$content .= $_;
	}
	close $filefile;
	
	print "HTTP/1.1 200 Ok\n";
	print "Content-type: application/x-www-form-urlencoded; name=\"" . $file->{ CRC } . "\"\n";
	print "Content-Disposition: attachment; filename=\"" . $file->{ OriginalName } . "\"\n";
	print "\n";
 
	print $content;
}

sub cash_box_upload
# //////////////////////////////////////////////////
{
	my ( $self, $task, $id, $template, $slist, $authip, $clientip ) = @_;

	my $vars = $self->{ 'VCS::Vars' };
	
	my $param = {};
	
	my $gconfig = $vars->getConfig( 'general' );
	
	$param->{ $_ } = ( $vars->getparam( $_ ) || 0 )
		for ( 'md5', 'appID', 'actnum', 'login', 'xerox', 'form', 'photo', 'print' );

        my $fobj = $vars->getparamfile('file');
	
	$vars->get_system->pheader( $vars );
		
	return print "ERROR|Неверный запрос на загрузку файла"
		unless $param->{ md5 } and $fobj and $param->{ appID } and $param->{ login };
           
        my $up_name = $gconfig->{ tmp_folder } . "cashbox_receipt/" . $param->{ md5 };
		
	my ( $file, $buffer );
	
	open $file, '>', $up_name or die;
	
	binmode $file;
	
	while ( my $bytesread = read( $fobj->{ file }, $buffer, 1024 ) ) {
	
		print $file $buffer;
		
		$buffer = "";
	} 
	
	close $file;  

	open my $file_check, '<', $up_name;
	
	my $md5file = Digest::MD5->new->addfile( $file_check )->hexdigest;
	
	close $file_check;
	
	if ( $md5file ne $param->{ md5 } ) { 
	   
		unlink $up_name;
               
		print "ERROR|Ошибка передачи файла";
	}
	else {
		$vars->db->query("
			INSERT INTO Cashboxes_receipt (Login, AppID, ActNum, Upload,
			CRC, OriginalName, Xerox, Form, Photo, Print) 
			VALUES (?, ?, ?, CURDATE(), ?, ?, ?, ?, ?, ?)", {},
			$param->{ login }, $param->{ appID }, $param->{ actnum }, $param->{ md5 }, $fobj->{ filename },
			$param->{ xerox }, $param->{ form }, $param->{ photo }, $param->{ print }
		);
		
		print "OK|Файл загружен";
	}
}

sub cash_box_appinfo
# //////////////////////////////////////////////////
{
	my ( $self, $task, $id, $template, $slist, $authip, $clientip ) = @_;

	my $vars = $self->{ 'VCS::Vars' };
	
	my $gconfig = $vars->getConfig('general');
	
	my $param = {};
	
	$param->{ $_ } = ( $vars->getparam( $_ ) || '' ) for ( 'app', 'summ', 'crc' );
	
	my $request_check = "app=" . $param->{ app } . "&summ=" . $param->{ summ };
		
	my $md5 = uc( Digest::MD5->new->add( $request_check )->hexdigest );
	
	return cash_box_output( $self, "ERROR|Контрольная сумма запроса неверна" ) unless $md5 eq $param->{ crc };
	
	my ( $person_arr, $ord_num ) = ( {}, 0 );

	if ( length( $param->{ app } ) == 15 ) {

		$person_arr = $vars->db->selallkeys("
			SELECT ID, AppNum, LName, FName, MName FROM Appointments WHERE AppNum = ?", $param->{ app }
		)->[0];
	}
	elsif ( length( $param->{ app } ) == 9 ) {
	
		$person_arr = $vars->db->selallkeys("
			SELECT Appointments.ID, AppNum, Appointments.LName, Appointments.FName, Appointments.MName
			FROM Appointments
			JOIN AppData ON Appointments.ID = AppData.AppID
			WHERE AppData.PassNum = ? GROUP BY Appointments.ID", 
			$param->{ app }
		)->[0];
	}
	else {
		$person_arr = {};
	}
	
	if ( $person_arr->{ ID } ) {
	
		$ord_num = $vars->db->sel1("
			SELECT COUNT(ID) FROM Cashboxes_receipt WHERE AppID = ?", $person_arr->{ ID }
		) || 0;
	}
	
	my $person = $person_arr->{ LName } . ' ' . $person_arr->{ FName } . ' ' . $person_arr->{ MName };
	
	my $appnum = $person_arr->{ AppNum };
	
	my $appid = $person_arr->{ ID };

	my $summ_text = $vars->admfunc->sum_to_russtr( 'RUR', $param->{ summ } );
	
	my $vat = $param->{ summ } * $gconfig->{'VAT'} / ( 100 + $gconfig->{'VAT'} );
	
	my $vat_text = $vars->admfunc->sum_to_russtr( 'RUR', $vat );

	return cash_box_output( $self, "OK|$person|$appnum|$summ_text|$vat_text|$ord_num|$appid" );
}

sub cash_box_mandocpack
# //////////////////////////////////////////////////
{
	my ( $self, $task, $id, $template, $slist, $authip, $clientip ) = @_;
	
	my $vars = $self->{ 'VCS::Vars' };
	
	my $gconfig = $vars->getConfig('general');
	
	my $param = {};
	my $serv_hash = {};
	
	my $data = { direct_docpack => 1 };
	
	$param->{ $_ } = ( $vars->getparam( $_ ) || '' )
		for ( 'login', 'pass', 'moneytype', 'money', 'services', 'center', 'vtype', 'callback', 'rdate', 'crc', 'r' );

	my $request_check = "login=" . $param->{ login } . "&pass=" . $param->{ pass } .
		"&moneytype=" . $param->{ moneytype } . "&money=" . $param->{ money } . "&center=" . $param->{ center } .
		"&vtype=" . $param->{ vtype } . "&rdate=" . $param->{ rdate } . "&services=" . $param->{ services } .
		"&callback=" . $param->{ callback } . "&r=" . ( $param->{ r } ? '1' : '0' );
	
	
	my $md5 = uc( Digest::MD5->new->add( $request_check )->hexdigest );
	
	return cash_box_output( $self, "ERROR|Контрольная сумма запроса неверна" ) unless $md5 eq $param->{ crc };
	
	return cash_box_output( $self, "ERROR|Не установлена кассовая интеграция" )
		unless $param->{ callback } =~ /^([0-9]{1,3}[\.]){3}[0-9]{1,3}$/;
		
	my $center_id = undef;

	if ( $param->{ r } && ( length( $param->{ center } ) == 15 ) ) {
	
		$param->{ center } =~ /^(\d{3})/;
		
		my $center_id_line = $1;
		
		$center_id_line =~ s/^[0]+//;
	
		$center_id = $vars->db->sel1("
			SELECT ID FROM Branches WHERE ID = ?", $center_id_line
		);
	}
	elsif ( $param->{ r } && ( length( $param->{ center } ) == 9 ) ) {
	
		$center_id = $vars->db->sel1("
			SELECT Branches.ID
			FROM Appointments
			JOIN AppData ON Appointments.ID = AppData.AppID
			JOIN Branches ON Appointments.CenterID = Branches.ID
			WHERE AppData.PassNum = ? 
			ORDER BY ID DESC LIMIT 1",
			$param->{ center }
		);
	}
	else {

		$center_id = $vars->db->sel1("
			SELECT ID FROM Branches WHERE BName = ?", $param->{ center }
		);
	}
	
	return cash_box_output( $self, "ERROR|Данные записи не найдены" ) unless $center_id;
		
	$data->{ center } = $center_id;
		
	my $rate_date = $vars->get_system->now_date();
	
	$rate_date = "$3-$2-$1" if $param->{ rdate } =~ /(\d{2})\.(\d{2})\.(\d{4})/;
	
	my $rate = $vars->admfunc->getRate( $vars, $gconfig->{'base_currency'}, $rate_date, $center_id );
	
	$data->{ concilPaymentDate } = $param->{ rdate };
	
	$data->{ ipdate } = $vars->get_system->now_date();
	
	$data->{ rate } = $vars->admfunc->getRate( $vars, $gconfig->{'base_currency'}, $rate_date, $center_id );
	
	( $data->{ vtype }, $data->{ vcat } ) = $vars->db->sel1("
		SELECT ID, category FROM VisaTypes WHERE VName = ?", $param->{ vtype }
	);

	my @serv_line = split( /\|/, $param->{ services } );

	$serv_hash->{ $_ } += 1 for @serv_line;
	
	my $urgent_docpack = 0;
	
	my $concil_index = 0;
	
	$data->{ applicants } = [];
	
	for ( keys %$serv_hash ) {
	
		if ( /^dhl=(.+)$/ ) {

			$data->{ shipsum } = $1;
			$data->{ newdhl } = 1;
			$data->{ shipping } = 1;
		}
		
		$data->{ insurance_manual_service } = $1 if /^insurance=(.+)$/;
		
		if ( /^(service|service_urgent)$/ ) {
		
			for my $n ( 0..($serv_hash->{ $_ } - 1) ) {
			
				$data->{ applicants }->[ $n ]->{ Status } = 1;
			}
			
			$urgent_docpack += ( $_ eq 'service_urgent' ? 1 : 0 );
		}
		
		if ( /^(concil|concil_urg_r|concil_n|concil_n_age)$/ ) {
		
			for my $n ( 0..($serv_hash->{ $_ } - 1) ) {
			
				$data->{ applicants }->[ $concil_index + $n ]->{ Concil } = 0 if /^(concil|concil_urg_r)$/;
				
				$data->{ applicants }->[ $concil_index + $n ]->{ AgeCatA } = 1 if /^concil_n_age$/;
				
				$data->{ applicants }->[ $concil_index + $n ]->{ iNRes } = 1 if /^concil_n$/;
				
				$data->{ applicants }->[ $concil_index + $n ]->{ direct_concil } = 1;
			}
			
			$concil_index += $serv_hash->{ $_ };
			
			$urgent_docpack += ( $_ eq 'concil_urg_r' ? 1 : 0 );
		}

		$data->{ $_ } = $serv_hash->{ $_ } if /^(vipsrv|sms_status|anketasrv|transum|printsrv|photosrv|xerox)$/;
		
		$data->{ $_ } = $serv_hash->{ $_ } if /^(srv1|srv2|srv3|srv4|srv5|srv6|srv7|srv8|srv9)$/;
	}

	$data->{ urgent } = ( $urgent_docpack ? 1 : 0 );
	
	my ( undef, undef, $mandocpack_failserv ) = send_docpack( $self, undef, $param->{ moneytype }, $param->{ money }, $data,
		$param->{ login }, $param->{ pass }, $param->{ callback }, $param->{ r } );
		
	return cash_box_output( $self, "WARNING|$mandocpack_failserv" ) if $mandocpack_failserv;

	return cash_box_output( $self, "OK|Запрос получен" );
}

sub get_access_for_cashbox_payment
# //////////////////////////////////////////////////
{
	my $self = shift;
	
	my $vars = $self->{ 'VCS::Vars' };

	my $interceptor_ip = $vars->db->sel1("
		SELECT InterceptorIP FROM Cashboxes_interceptors WHERE ID = ?", $vars->get_session->{ interceptor }
	) || undef;
	
	return ( $interceptor_ip =~ /^([0-9]{1,3}[\.]){3}[0-9]{1,3}$/ ? 1 : 0 );
}

sub cash_box_output
# //////////////////////////////////////////////////
{
	my ( $self, $msg ) = @_;
	
	my $vars = $self->{ 'VCS::Vars' };

	$vars->get_system->pheader( $vars );
	
	print $msg;
}

1;